using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class MeshRenderBrusher : EditorWindow
{
	static public Transform CurrentSelect;
	
	//路径
	string T4MEditorFolder = "Assets/TerrianAtlasBrusher/Editor/EditorIcons/";
	static public string T4MActived = "Activated";
	
	static int nbrT4MObj;
	static int T4MSelectID;
	static public int T4MMenuToolbar = 0;
	MeshRenderer[] T4MObjCounter;
	
	GUIContent[] MenuIcon = new GUIContent[7];
	GameObject UnityTerrain;
	static public Projector T4MPreview;
	
    [MenuItem ("Window/MeshRenderBrusher %t")]
    static void Initialize ()
    {
        MeshRenderBrusher window = (MeshRenderBrusher) EditorWindow.GetWindowWithRect(typeof(MeshRenderBrusher),
            new Rect(0, 0, 386, 582), false, "MeshRenderBrusher");
        window.Show();
    }

    /// <summary>
    /// 点击新物体后的初始化
    /// </summary>
    void IniNewSelect()
    {
	    //好像必须初始化，至少这部分是必要的
	    //UV是统一的
	    if (null == CurrentSelect.gameObject.GetComponent<MeshRenderer>()) return;
	    
	    T4MMaskTexUVCoord = CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial
		    .GetTextureScale("_BlendTex").x;
	    T4MMaskBlendTex = (Texture2D) CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial
		    .GetTexture("_BlendTex");
	    T4MMaskIndexTex = (Texture2D) CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial
		    .GetTexture("_IndexTex");
	    intialized = true;
    }

    void OnGUI()
    {
	    CurrentSelect = Selection.activeTransform;
	    nbrT4MObj = 0;
	    T4MObjCounter = GameObject.FindObjectsOfType(typeof(MeshRenderer)) as MeshRenderer[];
	    for (int i = 0; i < T4MObjCounter.Length; i++)
	    {
		    //if (T4MObjCounter[i].Master ==1)
		    nbrT4MObj = +1;
	    }

	    MenuIcon[0] =
		    new GUIContent(
			    AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Icons/conv.png", typeof(Texture2D)) as Texture);
	    MenuIcon[1] =
		    new GUIContent(
			    AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Icons/optimize.png", typeof(Texture2D)) as Texture);
	    MenuIcon[2] =
		    new GUIContent(
			    AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Icons/myt4m.png", typeof(Texture2D)) as Texture);
	    MenuIcon[3] =
		    new GUIContent(
			    AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Icons/paint.png", typeof(Texture2D)) as Texture);
	    MenuIcon[4] =
		    new GUIContent(
			    AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Icons/plant.png", typeof(Texture2D)) as Texture);
	    MenuIcon[5] =
		    new GUIContent(
			    AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Icons/lod.png", typeof(Texture2D)) as Texture);
	    MenuIcon[6] =
		    new GUIContent(
			    AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Icons/bill.png", typeof(Texture2D)) as Texture);

	    if (null == CurrentSelect 
	        || null == CurrentSelect.gameObject
	        ||  null == CurrentSelect.gameObject.GetComponent<MeshRenderer>()) return;
	    
	    if (CurrentSelect && Selection.activeInstanceID != T4MSelectID || UnityTerrain && T4MMenuToolbar != 0 ||
	        T4MMenuToolbar != 3 && T4MPreview)
	    {
		    IniNewSelect();
	    }

	    GUILayout.BeginHorizontal();
	    GUILayout.BeginArea(new Rect(0, 0, 90, 585));
	    GUILayout.Label(
		    AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/T4MBAN.jpg", typeof(Texture2D)) as Texture2D,
		    GUILayout.Width(24), GUILayout.Height(582));
	    GUILayout.EndArea();
	    GUILayout.BeginArea(new Rect(25, 0, 363, 585));
	    EditorGUILayout.Space();
	    GUILayout.BeginHorizontal("box");
	    T4MMenuToolbar = (int) GUILayout.Toolbar(T4MMenuToolbar, MenuIcon, "gridlist", GUILayout.Width(172),
		    GUILayout.Height(18));
	    GUILayout.FlexibleSpace();

	    GUILayout.Label("Controls", GUILayout.Width(52));
	    if (GUILayout.Button(T4MActived, GUILayout.Width(80)))
	    {
		    if (T4MActived == "Activated")
		    {
			    T4MActived = "Deactivated";
		    }
		    else
		    {
			    T4MActived = "Activated";
		    }
	    }

	    GUILayout.EndHorizontal();
	    GUILayout.Label(
		    AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/separator.png", typeof(Texture)) as Texture);

	    if (CurrentSelect != null && T4MActived == "Activated")
	    {

		    Renderer[] rendererPart = CurrentSelect.GetComponentsInChildren<Renderer>();

		    switch (T4MMenuToolbar)
		    {
			    case 0:
				    //ConverterMenu();
				    break;

			    case 1:
				    //Optimize();
				    break;

			    case 2:
				    //MyT4M();
				    break;

			    case 3:
				    PainterMenu();
				    break;

			    case 4:
				    //Planting ();
				    break;

			    case 5:
				    //afLOD();						
				    break;

			    case 6:
				    //BillboardMenu();
				    break;

		    }
	    }
	    else
	    {
		    GUILayout.FlexibleSpace();
		    GUILayout.BeginHorizontal();
		    GUILayout.FlexibleSpace();
		    GUILayout.Label(
			    AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/waiting.png", typeof(Texture)) as Texture);
		    GUILayout.FlexibleSpace();
		    GUILayout.EndHorizontal();
		    GUILayout.FlexibleSpace();
	    }

	    GUILayout.EndArea();
	    GUILayout.EndHorizontal();
    }


    #region Painter

    int MyT4MV = 0;
    string[] MyT4MMen = {"Painter"};
    
    void PainterMenu()
    {
	    if (CurrentSelect.GetComponent<MeshRenderer>())
	    {

		    GUILayout.BeginHorizontal();
		    GUILayout.FlexibleSpace();
		    MyT4MV = GUILayout.Toolbar(MyT4MV, MyT4MMen, GUILayout.Width(290), GUILayout.Height(20));
		    GUILayout.FlexibleSpace();
		    GUILayout.EndHorizontal();
		    switch (MyT4MV)
		    {
			    case 0:
				    //拒绝了一堆shader？
				    /*CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial.shader != Shader.Find("T4MShaders/ShaderModel2/Unlit/T4M World Projection Shader + LM") && 
					    CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial.shader != Shader.Find("T4MShaders/ShaderModel2/Diffuse/T4M World Projection Shader") &&
					    CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial.shader != Shader.Find("T4MShaders/ShaderModel2/MobileLM/T4M World Projection Shader_Mobile") && !CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial.HasProperty("_Tiling")*/
				    
				    if (true)
				    {
					    PixelPainterMenu();
				    }

				    break;
		    }

	    }
	    else
	    {
		    GUILayout.BeginHorizontal();
		    GUILayout.FlexibleSpace();
		    GUILayout.Label("Please, select the MeshRender", EditorStyles.boldLabel);
		    GUILayout.FlexibleSpace();
		    GUILayout.EndHorizontal();
	    }
    }
    
    Texture[] TexBrush;
    void IniBrush()
    {
	    ArrayList BrushList = new ArrayList ();
	    Texture  BrushesTL;
	    int BrushNum = 0;
	    do {
		    BrushesTL = (Texture) AssetDatabase.LoadAssetAtPath (T4MEditorFolder+"Brushes/Brush"+BrushNum+".png", typeof(Texture));
		    if (BrushesTL){
			    BrushList.Add (BrushesTL);
		    }
		    BrushNum++;
	    }while (BrushesTL);
	    TexBrush = BrushList.ToArray(typeof(Texture)) as Texture[];
    }
    
    void InitPincil()
	{
		if (CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial.HasProperty("_MainTex")){
			TexTexture = new Texture[9];
			for (int i = 0; i < 9; i++)
			{
				TexTexture[i] = GetAlertPartTexture(i);
			}
			//TexTexture[0]=AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_MainTex"))as Texture;
		}
	}

    static public int brushSize  = 1;//test
    static public float T4MStronger = 0.5f;
    int layerMask = 1 << 30;
    Texture[] TexTexture;
    static public int T4MselTexture =0;
    int selBrush =0;
    int oldBrush = 0;
    
    Vector2 Layer1Tile;
    Vector2 Layer2Tile;
    Vector2 Layer3Tile;
    Vector2 Layer4Tile;
    Vector2 Layer5Tile;
    Vector2 Layer6Tile;
    Vector2 Layer7Tile;
    Vector2 Layer8Tile;
    Vector2 Layer9Tile;
    Vector2 scrollPos;
    void InitPreview()
    {
	    var ProjectorB = new GameObject ("PreviewT4M");
		ProjectorB.AddComponent(typeof(Projector));
		ProjectorB.hideFlags = HideFlags.HideInHierarchy;
		T4MPreview= ProjectorB.GetComponent(typeof(Projector)) as Projector;
		MeshFilter SizeOfGeo = CurrentSelect.GetComponent<MeshFilter>();
		
		Vector2 MeshSize = new Vector2(SizeOfGeo.sharedMesh.bounds.size.x,SizeOfGeo.sharedMesh.bounds.size.z);
		T4MPreview.nearClipPlane = -20;
        T4MPreview.farClipPlane = 20;
        T4MPreview.orthographic = true;
		T4MPreview.orthographicSize = (brushSize* CurrentSelect.localScale.x)*(MeshSize.x/100);
		T4MPreview.ignoreLayers =  ~layerMask;
        T4MPreview.transform.Rotate(90, -90, 0);
        Material NewPMat = new Material(Shader.Find("T4MShaders/PreviewT4M/T4M 2 Textures Fix"));
		T4MPreview.material = NewPMat;
		T4MPreview.material.SetTexture("_MainTex", TexTexture[T4MselTexture]);
		T4MPreview.material.SetTexture("_MaskTex", TexBrush[selBrush]);
		if (T4MselTexture == 0){
			T4MPreview.material.SetTextureScale ("_MainTex", Layer1Tile);
		}
		else if (T4MselTexture == 1){
			T4MPreview.material.SetTextureScale ("_MainTex", Layer2Tile);
		}
		else if (T4MselTexture == 2){
			T4MPreview.material.SetTextureScale ("_MainTex", Layer3Tile);
		}
		else if (T4MselTexture == 3){
			T4MPreview.material.SetTextureScale ("_MainTex", Layer4Tile);
		}
		else if (T4MselTexture == 4 ){
			T4MPreview.material.SetTextureScale ("_MainTex", Layer5Tile);
		}
		else if (T4MselTexture == 5 ){
			T4MPreview.material.SetTextureScale ("_MainTex", Layer6Tile);
		}
		else if (T4MselTexture == 6 ){
			T4MPreview.material.SetTextureScale ("_MainTex", Layer7Tile);
		}
		else if (T4MselTexture == 7 ){
			T4MPreview.material.SetTextureScale ("_MainTex", Layer8Tile);
		}
		else if (T4MselTexture == 8 ){
			T4MPreview.material.SetTextureScale ("_MainTex", Layer9Tile);
		}
		T4MPreview.material.SetTexture("_MainTex", TexTexture[T4MselTexture]);
    }

    private static int mRowOrColCount = 3;
    public static int MaxTexNum = mRowOrColCount * mRowOrColCount;
    private Texture GetAlertPartTexture(int index)
    {
	    //为什么Tex是128大小？？？明明已经1024不压缩了
	    //AssetPreview.GetAssetPreview这真的坑，到处都是坑
	    //Todo:这里初始化顺序有问题，第一次打开会是null
	    Texture mainTex = CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_MainTex");
	    if (null == mainTex) return null;
		//默认原图为1024(1k)的Alert--x
		//只能，这么处理一下
		int mSingleTexSize = 340;
	    
	    
	    Texture2D texture2D = new Texture2D(mSingleTexSize, mSingleTexSize, TextureFormat.RGBA32, false);

	    RenderTexture currentRT = RenderTexture.active;
  
	    RenderTexture renderTexture = RenderTexture.GetTemporary(mainTex.width, mainTex.height, 32);
	    Graphics.Blit(mainTex, renderTexture);

	    RenderTexture.active = renderTexture;
	    //ReadPixels是从左上角为(0,0)，但是TearrainAtlas是从左下角，向右开始弄的图
	    //所以在在下面写个Rect的方法吧
	    texture2D.ReadPixels(TransformRect(index, mSingleTexSize, mainTex.width), 0, 0);
	    texture2D.Compress(false);
	    texture2D.Apply();

	    RenderTexture.active = currentRT;
	    RenderTexture.ReleaseTemporary(renderTexture);

	    return texture2D;
    }

    private Rect TransformRect(int index, int singleTexSize, int mainTexSize)
    {
	    int row = 1 + (index / mRowOrColCount);
	    int col = index % mRowOrColCount;
	    return new Rect(col * singleTexSize + 1, mainTexSize - row * singleTexSize - 1, singleTexSize, singleTexSize);
    }

    public enum PaintHandle{
	    Classic,
	    Follow_Normal_Circle,
	    Follow_Normal_WireCircle,
	    Hide_preview
    }	
    static public PaintHandle PaintPrev = PaintHandle.Classic;
    
    bool intialized=false;
    bool joinTiles = true;
    Color ShinessColor;
    float shiness0;
    float shiness1;
    float shiness2;
    float shiness3;
    
    static public float T4MMaskTexUVCoord =1f;
    static public int T4MBrushSizeInPourcent;
    //Todo:本来想做个边缘strong值，来优化。但是好像可以先不管，以后有需求再说
    static public int T4MBrushSizeAtBound;
    
    int OldT4MBrushSizeInPourcent;
    int OldT4MBrushSizeAtBound;
    static public float[] T4MBrushAlpha;
    
    static public Texture2D T4MMaskBlendTex;
    static public Texture2D T4MMaskIndexTex;
    
    void PixelPainterMenu()
    {
	    if (CurrentSelect.GetComponent<MeshRenderer>())
	    {
		    if (CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial &&
		        CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial.HasProperty("_IndexTex") &&
		        CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial.HasProperty("_BlendTex") &&
		        CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial.HasProperty("_MainTex"))
		    {
			    IniBrush();
			    InitPincil();
			    if (!T4MPreview)
				    InitPreview();
			    if (intialized)
			    {
				    GUILayout.BeginHorizontal();
				    GUILayout.FlexibleSpace();
				    GUILayout.Label(
					    AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/brushes.jpg", typeof(Texture)) as Texture,
					    "label");
				    GUILayout.BeginHorizontal("box", GUILayout.Width(318));
				    GUILayout.FlexibleSpace();
				    oldBrush = selBrush;
				    selBrush = GUILayout.SelectionGrid(selBrush, TexBrush, 9, "gridlist", GUILayout.Width(290),
					    GUILayout.Height(70));
				    GUILayout.FlexibleSpace();
				    GUILayout.EndHorizontal();
				    GUILayout.FlexibleSpace();
				    GUILayout.EndHorizontal();

				    GUILayout.BeginHorizontal();
				    GUILayout.FlexibleSpace();
				    GUILayout.BeginHorizontal("box", GUILayout.Width(340));
				    GUILayout.FlexibleSpace();
				    T4MselTexture = GUILayout.SelectionGrid(T4MselTexture, TexTexture, 6, "gridlist",
					    GUILayout.Width(340), GUILayout.Height(64));
				    GUILayout.FlexibleSpace();
				    GUILayout.EndHorizontal();
				    GUILayout.FlexibleSpace();
				    GUILayout.EndHorizontal();

				    EditorGUILayout.Space();


				    GUILayout.BeginHorizontal();
				    GUILayout.FlexibleSpace();
				    GUILayout.BeginVertical("box", GUILayout.Width(347));
				    GUILayout.BeginHorizontal();
				    GUILayout.Label("Preview Type", GUILayout.Width(145));
				    PaintPrev = (PaintHandle) EditorGUILayout.EnumPopup(PaintPrev, GUILayout.Width(160));
				    GUILayout.EndHorizontal();
				    brushSize = (int) EditorGUILayout.Slider("Brush Size", brushSize, 1, 36);
				    T4MStronger = EditorGUILayout.Slider("Brush Stronger", T4MStronger, 0.01f, 2f);
				    GUILayout.EndVertical();
				    GUILayout.FlexibleSpace();
				    GUILayout.EndHorizontal();

				    if (CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial.HasProperty("_SpecColor"))
				    {
					    EditorGUILayout.Space();
					    GUILayout.BeginHorizontal();
					    GUILayout.FlexibleSpace();
					    GUILayout.BeginVertical("box", GUILayout.Width(347), GUILayout.Height(96));
					    ShinessColor = EditorGUILayout.ColorField("Shininess Color", ShinessColor);
					    CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial
						    .SetColor("_SpecColor", ShinessColor);
					    EditorGUILayout.Space();
					    if (CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial.HasProperty("_ShininessL0"))
					    {
						    shiness0 = EditorGUILayout.Slider("Shininess Layer 1", shiness0, 0.00f, 1.0f);
						    CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial
							    .SetFloat("_ShininessL0", shiness0);
					    }

					    if (CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial.HasProperty("_ShininessL1"))
					    {
						    shiness1 = EditorGUILayout.Slider("Shininess Layer 2", shiness1, 0.00f, 1.0f);
						    CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial
							    .SetFloat("_ShininessL1", shiness1);
					    }

					    if (CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial.HasProperty("_ShininessL2"))
					    {
						    shiness2 = EditorGUILayout.Slider("Shininess Layer 3", shiness2, 0.00f, 1.0f);
						    CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial
							    .SetFloat("_ShininessL2", shiness2);
					    }

					    if (CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial.HasProperty("_ShininessL3"))
					    {
						    shiness3 = EditorGUILayout.Slider("Shininess Layer 4", shiness3, 0.00f, 1.0f);
						    CurrentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial
							    .SetFloat("_ShininessL3", shiness3);
					    }

					    GUILayout.EndVertical();
					    GUILayout.FlexibleSpace();
					    GUILayout.EndHorizontal();

				    }

				    EditorGUILayout.Space();

				    if (TexBrush.Length > 0)
				    {
					    T4MPreview.material.SetTexture("_MaskTex", TexBrush[selBrush]);
					    MeshFilter temp = CurrentSelect.GetComponent<MeshFilter>();
					    if (temp == null)
						    temp = CurrentSelect.GetComponent<MeshFilter>();
					    T4MPreview.orthographicSize =
						    (brushSize * CurrentSelect.localScale.x) * (temp.sharedMesh.bounds.size.x / 200);
				    }

				    float test = T4MStronger * 200 / 100;
				    T4MPreview.material.SetFloat("_Transp", Mathf.Clamp(test, 0.4f, 1));

				    T4MBrushSizeInPourcent = Mathf.RoundToInt((brushSize * T4MMaskBlendTex.width) / 100);

				    if (T4MBrushAlpha == null || OldT4MBrushSizeInPourcent != T4MBrushSizeInPourcent ||
				        OldT4MBrushSizeAtBound != T4MBrushSizeAtBound || oldBrush != selBrush)
				    {
					    T4MBrushAlpha = new float[T4MBrushSizeInPourcent * T4MBrushSizeInPourcent];
					    Texture2D TBrush = TexBrush[selBrush] as Texture2D;

					    for (int i = 0; i < T4MBrushSizeInPourcent; i++)
					    {
						    for (int j = 0; j < T4MBrushSizeInPourcent; j++)
						    {
							    T4MBrushAlpha[j * T4MBrushSizeInPourcent + i] = 
								    (TBrush.GetPixelBilinear(((float) i) / T4MBrushSizeInPourcent,((float) j) / T4MBrushSizeInPourcent).a)
								    ;
							    //神特么坑的加权
									// * 0.8f + 0.2f;
						    }
					    }

					    oldBrush = selBrush;
					    OldT4MBrushSizeInPourcent = T4MBrushSizeInPourcent;
					    OldT4MBrushSizeAtBound = T4MBrushSizeAtBound;
				    }

				    if (T4MselTexture == 0)
					    T4MPreview.material.SetTextureScale("_MainTex", Layer1Tile);
				    else if (T4MselTexture == 1)
					    T4MPreview.material.SetTextureScale("_MainTex", Layer2Tile);
				    else if (T4MselTexture == 2)
					    T4MPreview.material.SetTextureScale("_MainTex", Layer3Tile);
				    else if (T4MselTexture == 3)
					    T4MPreview.material.SetTextureScale("_MainTex", Layer4Tile);
				    else if (T4MselTexture == 4)
					    T4MPreview.material.SetTextureScale("_MainTex", Layer5Tile);
				    else if (T4MselTexture == 5)
					    T4MPreview.material.SetTextureScale("_MainTex", Layer6Tile);
				    else if (T4MselTexture == 6)
					    T4MPreview.material.SetTextureScale("_MainTex", Layer7Tile);
				    else if (T4MselTexture == 7)
					    T4MPreview.material.SetTextureScale("_MainTex", Layer8Tile);
				    else if (T4MselTexture == 8)
					    T4MPreview.material.SetTextureScale("_MainTex", Layer9Tile);

				    GUILayout.BeginHorizontal();
				    GUILayout.FlexibleSpace();
				    if (GUILayout.Button("Refresh Textures"))
				    {
					    RefreshCurSelectedMaterialTextures();
				    }
				    GUILayout.FlexibleSpace();
				    GUILayout.EndHorizontal();
			    }
		    }
	    }
	    else
	    {
		    GUILayout.BeginHorizontal();
		    GUILayout.FlexibleSpace();
		    GUILayout.Label("Please, select the MeshRender", EditorStyles.boldLabel);
		    GUILayout.FlexibleSpace();
		    GUILayout.EndHorizontal();
	    }
    }

    public static void SaveTexture()
    {
	    var path = AssetDatabase.GetAssetPath(T4MMaskIndexTex);
	    var bytes = T4MMaskIndexTex.EncodeToPNG();
	    File.WriteAllBytes(path, bytes);

	    var path2 = AssetDatabase.GetAssetPath(T4MMaskBlendTex);
	    var bytes2 = T4MMaskBlendTex.EncodeToPNG();
	    File.WriteAllBytes(path2, bytes2);
    }

    #region 整张图刷新-Blend<0.001f的都清0

    //万般无奈出此下策，
    //刷地形刷到第四种颜色容易崩怎么办？整体刷新一下
    //Q:想不到更好的处理方法
    private static void RefreshCurSelectedMaterialTextures()
    {
	    MeshRenderBrusherExtends.RefreshTextures();
	    SaveTexture();
    }

    #endregion --整张图刷新-Blend<0.001f的都清0
    

    #endregion
}
