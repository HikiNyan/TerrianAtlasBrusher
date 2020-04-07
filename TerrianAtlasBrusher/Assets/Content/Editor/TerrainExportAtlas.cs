using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using UnityEditor.VersionControl;
using FileMode = System.IO.FileMode;

public class TerrainExportAtlas : EditorWindow
{
    enum RESOLUTIONS
    {
        OneK = 0,
        TwoK = 1,
        FourK = 2
    }

    int _maxTexNum = 0;
    int _secondTexNum = 0;
    int _thirdTexNum = 0;

    float _maxChannelWeight = 0f;
    float _secondChannelWeight = 0f;
    float _thirdChannelWeight = 0f;

    private Terrain _sourceTerrain;
    private Material _targetMaterial;

    private int _baseMapResolution = 1020;
    private int BasemapResolution
    {
        get
        {
            switch (_resolution)
            {
                case RESOLUTIONS.TwoK:
                    return _baseMapResolution;// * 2;
                case RESOLUTIONS.FourK:
                    return _baseMapResolution;// * 4;
            }
            return _baseMapResolution;
        }
    }

    private RESOLUTIONS _resolution = RESOLUTIONS.OneK;

    const int COLUMN_AND_ROW_COUNT = 3;
    const int MIN_SPACE_SIZE = 4;

    const string DIRECTORYNAME = "/TerrianAtlasBrusher/Exports";

    string _directoryName = "";

    [MenuItem("Window/TerrainAtlas/ExportMaps")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(TerrainExportAtlas));
    }

    void OnGUI()
    {
        GenericMenu menu = new GenericMenu();

        _sourceTerrain = (Terrain)EditorGUILayout.ObjectField("Select Terrain", _sourceTerrain, typeof(Terrain), true);
        GUILayout.Space(10);

        //提供导出按钮
        if (_sourceTerrain != null)
        {
            GUILayout.Label("Save Directory: Assets" + DIRECTORYNAME + "/" + _directoryName + "/");
            GUILayout.Space(10);

            _directoryName = GUILayout.TextField(_directoryName);

            //图片导出分辨率
            _resolution = (RESOLUTIONS)EditorGUILayout.EnumPopup("Base Map Resolution", _resolution);

            GUILayout.Space(10);

            //导出主贴图按钮
            if (GUILayout.Button("Export Base Map"))
            {
                ExportBasemap();
                ExportIndexAndWeightMap();
            }

            //重新导入主贴图按钮
            if (GUILayout.Button("Reimport Base Map"))
            {
                ReimportBasemap();
                ReimportIndexAndWeightMap();
            }

            /*GUILayout.Space(10);

            //导出BlendMap & IndexMap按钮
            if (GUILayout.Button("Export Index And Blend Map"))
            {
                ExportIndexAndWeightMap();
            }
            
            //重新导入Blend & Index Map
            //需要有这两张图
            if (GUILayout.Button("Reimport Index And Blend Map"))
            {
                ReimportIndexAndWeightMap();
            }*/
            
            
            //自动复制Tiling功能部分
            GUILayout.Space(20);
            _targetMaterial = (Material)EditorGUILayout.ObjectField("Select Material", _targetMaterial, typeof(Material), true);
            GUILayout.Space(10);

            if (null != _targetMaterial)
            {
                if (GUILayout.Button("Copy Tiling from Terrian to Material"))
                {
                    CopyTiling();
                }
            }
        }
        else
        {
            GUILayout.Label("Please select a terrain!");
        }
    }

    #region 导出主贴图按钮
    
    void ExportBasemap()
    {
        TerrainData _terrainData = _sourceTerrain.terrainData;
        SplatPrototype[] prototypeArray = _terrainData.splatPrototypes;

        //创建相应数目的RT
        RenderTexture[] rtArray = new RenderTexture[prototypeArray.Length];

        int texSize = BasemapResolution / COLUMN_AND_ROW_COUNT;
        int sourceSize = texSize - 2 * MIN_SPACE_SIZE;
        Texture2D[] texArray = new Texture2D[prototypeArray.Length];
        Texture2D[] normalMapArray = new Texture2D[prototypeArray.Length];

        for (int i = 0; i < prototypeArray.Length; i++)
        {
            rtArray[i] = RenderTexture.GetTemporary(sourceSize, sourceSize, 24);
            texArray[i] = new Texture2D(sourceSize, sourceSize, TextureFormat.RGB24, false);
            normalMapArray[i] = new Texture2D(sourceSize, sourceSize, TextureFormat.RGB24, false);
        }

        //使用一个UnlitShader来将贴图绘制到具体的RenderTexture上
        /*Shader shader = Shader.Find("Unlit/UnlitShader");
        Material material = new Material(shader);*/

        //将这些图读入相应数目的Tex2D中
        for (int i = 0; i < prototypeArray.Length; i++)
        {
            //fujiajie说，直接读Texture原图，不压缩
            //Blit可能做差值压缩
            /*Graphics.Blit(prototypeArray[i].texture, rtArray[i], material, 0);
            RenderTexture.active = rtArray[i];
            texArray[i].ReadPixels(new Rect(0f, 0f, (float)texSize, (float)texSize), 0, 0);*/

            ReadTexToTex(prototypeArray[i].texture, texArray[i]);
            //如果有法线贴图，则将这些值读入
            if (prototypeArray[i].normalMap != null)
            {
                /*Graphics.Blit(prototypeArray[i].normalMap, rtArray[i], material, 0);
                RenderTexture.active = rtArray[i];
                normalMapArray[i].ReadPixels(new Rect(0f, 0f, (float)texSize, (float)texSize), 0, 0);*/
                ReadTexToTex(prototypeArray[i].normalMap, normalMapArray[i]);
            }
        }


        //生成一个目标分辨率的图集
        Texture2D baseTex = new Texture2D(BasemapResolution, BasemapResolution, TextureFormat.RGB24, false);
        Texture2D normalTex = new Texture2D(BasemapResolution, BasemapResolution, TextureFormat.RGB24, false);

        for (int i = 0; i < prototypeArray.Length; i++)
        {
            //需要根据图片的序号算出当前贴图在图集中的起始位置
            int columnNum = i % COLUMN_AND_ROW_COUNT;
            int rowNum = (i % (COLUMN_AND_ROW_COUNT * COLUMN_AND_ROW_COUNT)) / COLUMN_AND_ROW_COUNT;
            int startWidth = columnNum * texSize;
            int startHeight = rowNum * texSize;
            for (int j = 0; j < texSize; j++)
            {
                for (int k = 0; k < texSize; k++)
                {
                    Color color = GetPixelColor(j, k, texArray[i]);
                    baseTex.SetPixel(startWidth + j, startHeight + k, color);

                    Color normalColor = (prototypeArray[i].normalMap == null) ? new Color(0.5f, 0.5f, 1) : GetPixelColor(j, k, normalMapArray[i]);
                    normalTex.SetPixel(startWidth + j, startHeight + k, normalColor);
                }
            }
        }
        
        baseTex.Apply();
        // Encode texture into PNG
        byte[] bytes = baseTex.EncodeToPNG();
        string directoryPath = Application.dataPath + DIRECTORYNAME + "/" + _directoryName + "/";
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        File.WriteAllBytes(directoryPath + "MainTex.png", bytes);

        normalTex.Apply();
        // Encode texture into PNG
        byte[] normalBytes = normalTex.EncodeToPNG();
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        File.WriteAllBytes(directoryPath + "NormalTex.png", normalBytes);

        Debug.Log("BaseMap And NormalMap Exported");
        
        
        //AssetImporter.GetAtPath()
    }

    private void ReadTexToTex(Texture2D source, Texture2D target)
    {
        string path = AssetDatabase.GetAssetPath(source);
        var ti = (TextureImporter) AssetImporter.GetAtPath(path);
        ti.isReadable = true;
        AssetDatabase.ImportAsset(path);

        for (int i = 0; i < source.width; i++)
        {
            if (target.width <= i) continue;
            for (int j = 0; j < source.height; j++)
            {
                if (target.height <= j) continue;
                
                target.SetPixel(i, j, source.GetPixel(i, j));
            }
        }
    }

    #endregion --导出主贴图按钮

    #region 重新导入主贴图按钮

    void ReimportBasemap()
    {
        string directoryPath = Application.dataPath + DIRECTORYNAME + "/" + _directoryName + "/";

        ReImoprtBasemap(directoryPath + "MainTex.png");
        ReImoprtBasemap(directoryPath + "NormalTex.png");
    }

    private void ReImoprtBasemap(string path)
    {
        try
        {
            string metaPath = path + ".meta";
            FileStream fs = new FileStream(metaPath, FileMode.Open);
            byte[] bytes = new byte[fs.Length]; //一个meta不可能超21e的吧。。。
            fs.Read(bytes, 0, (int) fs.Length);
            string s = new UTF8Encoding().GetString(bytes);
            s = s.Replace("enableMipMap: 1", "enableMipMap: 0");
            s = s.Replace("nPOTScale: 1", "nPOTScale: 0");
            
            byte[] newBytes = new UTF8Encoding().GetBytes(s);
            fs.Close();
            File.Delete(metaPath);
            fs = new FileStream(metaPath, FileMode.Create);
            fs.Write(newBytes, 0, newBytes.Length);
            fs.Close();
        }
        catch (IOException e)
        {
            Debug.LogError("没有对应meta文件，请先生成对应png，且打开Unity后，再点击");
        }
    }

    #endregion --重新导入主贴图按钮
    
    #region 导出BlendMap & IndexMap按钮
    
    private static int mRowAndColNum = 3;
    private static float mMaxNum = mRowAndColNum * mRowAndColNum;
    
    void ExportIndexAndWeightMap()
    {
        TerrainData _terrainData = _sourceTerrain.terrainData;
        SplatPrototype[] prototypeArray = _terrainData.splatPrototypes;
        int _textureNum = prototypeArray.Length;

        //获取混合贴图
        Texture2D[] alphaMapArray = _terrainData.alphamapTextures;
        int witdh = alphaMapArray[0].width;
        int height = alphaMapArray[0].height;

        //新建和混合贴图一样大小的贴图
        Texture2D indexTex = new Texture2D(witdh, height, TextureFormat.RGB24, false, true);
        Color indexColor = new Color(0, 0, 0, 0);

        Texture2D blendTex = new Texture2D(witdh, height, TextureFormat.RGB24, false, true);
        Color blendColor = new Color(0, 0, 0, 0);

        //对每一个像素进行计算
        for (int j = 0; j < witdh; j++)
        {
            for (int k = 0; k < height; k++)
            {
                //默认都是第一个贴图
                //这里支持将三层索引的信息导出，可供后续的Shader使用
                ResetNumAndWeight();

                //遍历所有Control的所有通道，识别出最大的通道所在的贴图序号
                for (int i = 0; i < _textureNum; i++)
                {
                    //根据贴图的序号算出当前应该计算的是哪个值
                    int controlMapNumber = (i % 16) / 4;
                    int controlChannelNum = i % 4;
                    Color color = alphaMapArray[controlMapNumber].GetPixel(j, k);
                    switch (controlChannelNum)
                    {
                        case 0:
                            CalculateIndex(i, color.r);
                            break;
                        case 1:
                            CalculateIndex(i, color.g);
                            break;
                        case 2:
                            CalculateIndex(i, color.b);
                            break;
                        case 3:
                            CalculateIndex(i, color.a);
                            break;
                        default:
                            break;
                    }
                }

                //将识别出来的序号写入IndexMap的通道中
                //需将此值转换到(0, 1)的范围内，因为最多支持16张贴图，而序号是0到15，则除以15即可
                indexColor.r = _maxTexNum / (mMaxNum - 1f);
                indexColor.g = _secondTexNum / (mMaxNum - 1f);
                indexColor.b = _thirdTexNum / (mMaxNum - 1f);
                indexTex.SetPixel(j, k, indexColor);

                //计算Blend因子，将其填入到贴图通道中
                blendColor.r = _maxChannelWeight;
                blendColor.g = _secondChannelWeight;
                blendColor.b = _thirdChannelWeight;
                blendTex.SetPixel(j, k, blendColor);
            }
        }

        string directoryPath = Application.dataPath + DIRECTORYNAME + "/" + _directoryName + "/";
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        //这个修改也没用。。果然需要write以后再Import嘛。。。
        indexTex.filterMode = FilterMode.Point;
        indexTex.Apply();
        byte[] bytes = indexTex.EncodeToPNG();
        File.WriteAllBytes(directoryPath + "IndexTex.png", bytes);

        blendTex.Apply();
        byte[] blendBytes = blendTex.EncodeToPNG();
        File.WriteAllBytes(directoryPath + "BlendTex.png", blendBytes);

        Debug.Log("IndexMap And BlendMap Exported");
    }

    #endregion --导出BlendMap & IndexMap按钮
    
    #region 重新导入Blend & Index Map
    
    void ReimportIndexAndWeightMap()
    {
        string directoryPath = Application.dataPath + DIRECTORYNAME + "/" + _directoryName + "/";
        
        ReImportTex(directoryPath + "IndexTex.png", true);
        ReImportTex(directoryPath + "BlendTex.png");
    }

    private void ReImportTex(string path, bool isIndexTex = false)
    {
        //好像，meta文件不让直接修改，很烦
        //这里修改Meta文件需求先删除再创建，无法直接修改（可能是编译器打开着的原因，这个也是前辈文章中提示的我没有验证，不管用何种方式能修改Meta文件即可）；
        //好的，可以改了。果然直接改meta才好用
        try
        {
            string metaPath = path + ".meta";
            FileStream fs = new FileStream(metaPath, FileMode.Open);
            byte[] bytes = new byte[fs.Length]; //一个meta不可能超21e的吧。。。
            fs.Read(bytes, 0, (int) fs.Length);
            string s = new UTF8Encoding().GetString(bytes);
            s = s.Replace("isReadable: 0", "isReadable: 1");
            s = s.Replace("textureCompression: 1", "textureCompression: 0");
            s = s.Replace("sRGBTexture: 1", "sRGBTexture: 0");
            
            if (isIndexTex)
            {
                s = s.Replace("filterMode: -1", "filterMode: 0");
            }

            byte[] newBytes = new UTF8Encoding().GetBytes(s);
            fs.Close();
            File.Delete(metaPath);
            fs = new FileStream(metaPath, FileMode.Create);
            fs.Write(newBytes, 0, newBytes.Length);
            fs.Close();
        }
        catch (IOException e)
        {
            Debug.LogError("没有对应meta文件，请先生成对应png，且打开Unity后，再点击");
        }
    }

    void ResetNumAndWeight()
    {
        _maxTexNum = 0;
        _secondTexNum = 0;
        _thirdTexNum = 0;

        _maxChannelWeight = 0f;
        _secondChannelWeight = 0f;
        _thirdChannelWeight = 0f;
    }

    void CalculateIndex(int index, float curWeight)
    {
        //如果比最大的元素大，则取当前为最大，取之前第一为第二，取之前第二的为第三
        if (curWeight >= _maxChannelWeight)
        {
            _thirdChannelWeight = _secondChannelWeight;
            _thirdTexNum = _secondTexNum;

            _secondChannelWeight = _maxChannelWeight;
            _secondTexNum = _maxTexNum;

            _maxChannelWeight = curWeight;
            _maxTexNum = index;
        }
        //如果仅是比第二的元素大，则取当前为第二，取之前的第二为第三
        else if (curWeight >= _secondChannelWeight)
        {
            _thirdChannelWeight = _secondChannelWeight;
            _thirdTexNum = _secondTexNum;

            _secondChannelWeight = curWeight;
            _secondTexNum = index;
        }
        //如果仅是比第三的元素大，则取当前为第三
        else if (curWeight >= _thirdChannelWeight)
        {
            _thirdChannelWeight = curWeight;
            _thirdTexNum = index;
        }
    }

    Color GetPixelColor(int rowNum, int columnNum, Texture2D oriTex)
    {
        Color oriColor;

        int minNum = MIN_SPACE_SIZE;
        int maxNum = BasemapResolution / COLUMN_AND_ROW_COUNT - minNum + 1;

        int sourceRowNum = rowNum - minNum;
        if (sourceRowNum <= 0)
        {
            sourceRowNum += oriTex.height;
        }
        else if (sourceRowNum > oriTex.height)
        {
            sourceRowNum -= oriTex.height;
        }
        int sourceColumnNum = columnNum - minNum;
        if (sourceColumnNum <= 0)
        {
            sourceColumnNum += oriTex.height;
        }
        else if (sourceColumnNum > oriTex.width)
        {
            sourceColumnNum -= oriTex.width;
        }

        oriColor = oriTex.GetPixel(sourceRowNum, sourceColumnNum);
        return oriColor;
    }
    #endregion --重新导入Blend & Index Map

    #region 自动复制Tiling功能部分

    void CopyTiling()
    {
        if (null == _sourceTerrain 
            || null == _targetMaterial 
            || _targetMaterial.shader != Shader.Find("TerrainAtlas"))
        {
            EditorUtility.DisplayDialog("", "拖Terrain/Material", "知道了");
        }

        TerrainData _terrainData = _sourceTerrain.terrainData;
        SplatPrototype[] prototypeArray = _terrainData.splatPrototypes;
        Vector4[] resultTiling = new Vector4[3];
        for (int i = 0; i < prototypeArray.Length; i++)
        {
            int index = i / 3;
            switch (i % 3)
            {
                case 0:
                    resultTiling[index].x = prototypeArray[i].tileSize.x;
                    break;
                case 1:
                    resultTiling[index].y = prototypeArray[i].tileSize.x;
                    break;
                case 2:
                    resultTiling[index].z = prototypeArray[i].tileSize.x;
                    break;
            }
        }
        _targetMaterial.SetVector("_tilling13", resultTiling[0]);
        _targetMaterial.SetVector("_tilling46", resultTiling[1]);
        _targetMaterial.SetVector("_tilling79", resultTiling[2]);
    }

    #endregion --自动复制Tiling功能部分
}
