
//这个，会有问题嘛。。。如果只在Scene里面处理，应该没啥问题吧

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BoxCollider))]
[CanEditMultipleObjects]
public class MeshRenderBrusherExtends : UnityEditor.Editor
{
	int layerMask = 1 << 30;
    
    Texture2D[] UndoObj;
    private static List<MRBPixelModel> mPixelList = new List<MRBPixelModel>();
    private static List<MRBPixelModel> mPixelIndexList = new List<MRBPixelModel>();
    //能省就省，初始化数值随便定的
    //命名太长了，原mPixelBounderCalculator
    private static TexAreaCalWithBounder mBounderCal = new TexAreaCalWithBounder(0, 0, 511, 511, 511);
    
    GameObject PlantObjPreview;
    
    bool ToggleF = false;

    
    int State;
    int oldState;

    void OnSceneGUI()
    {
        if (MeshRenderBrusher.T4MPreview && MeshRenderBrusher.T4MMenuToolbar == 3)
            Painter();
    }

    void Painter()
    {
        if (State != 1)
            State = 1;
        
        Event e  = Event.current;
        if (e.type ==  EventType.KeyDown && e.keyCode==KeyCode.T){
            if (MeshRenderBrusher.T4MActived != "Activated")
                MeshRenderBrusher.T4MActived = "Activated";
            else MeshRenderBrusher.T4MActived = "Deactivated";
        }
        /*if (MeshRenderBrusher.T4MPreview && MeshRenderBrusher.T4MActived == "Activated" && MeshRenderBrusher.T4MPreview.enabled == false || MeshRenderBrusher.T4MPreview.enabled == false){
            if(
                MeshRenderBrusher.PaintPrev != MeshRenderBrusher.PaintHandle.Follow_Normal_Circle && 
                MeshRenderBrusher.PaintPrev != MeshRenderBrusher.PaintHandle.Follow_Normal_WireCircle &&
                MeshRenderBrusher.PaintPrev != MeshRenderBrusher.PaintHandle.Hide_preview
            ){
                MeshRenderBrusher.T4MPreview.enabled = true;
            }
        }else if (MeshRenderBrusher.T4MPreview && MeshRenderBrusher.T4MActived == "Deactivated" && MeshRenderBrusher.T4MPreview.enabled == true || MeshRenderBrusher.T4MPreview.enabled == true){	
            if (MeshRenderBrusher.PaintPrev != MeshRenderBrusher.PaintHandle.Classic){ 
                MeshRenderBrusher.T4MPreview.enabled = false;
            }
        }*/
        //暂时这个大小有问题，先设定成false
        MeshRenderBrusher.T4MPreview.enabled = false;

        if (MeshRenderBrusher.T4MActived == "Activated")
        {
            HandleUtility.AddDefaultControl(0);
            RaycastHit raycastHit = new RaycastHit();
            Ray terrain = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.KeypadPlus)
            {
                MeshRenderBrusher.brushSize += 1;
            }
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.KeypadMinus)
            {
                MeshRenderBrusher.brushSize -= 1;
            }

            if (Physics.Raycast(terrain, out raycastHit, Mathf.Infinity, layerMask))
            {
	            if (null == MeshRenderBrusher.T4MPreview || null == MeshRenderBrusher.CurrentSelect) return;
	            
	            
                if (false)//MeshRenderBrusher.CurrentSelect.gameObject.GetComponent<T4MObjSC>().ConvertType != "UT")
					MeshRenderBrusher.T4MPreview.transform.localEulerAngles =
						new Vector3(90, 180 + MeshRenderBrusher.CurrentSelect.localEulerAngles.y, 0);
				else
					MeshRenderBrusher.T4MPreview.transform.localEulerAngles =
						new Vector3(90, -90 + MeshRenderBrusher.CurrentSelect.localEulerAngles.y, 0);
				MeshRenderBrusher.T4MPreview.transform.position = raycastHit.point;

				if (MeshRenderBrusher.PaintPrev != MeshRenderBrusher.PaintHandle.Classic && MeshRenderBrusher.PaintPrev != MeshRenderBrusher.PaintHandle.Hide_preview &&
				    MeshRenderBrusher.PaintPrev != MeshRenderBrusher.PaintHandle.Follow_Normal_WireCircle)
				{
					Handles.color = new Color(1f, 1f, 0f, 0.05f);
					Handles.DrawSolidDisc(raycastHit.point, raycastHit.normal,
						MeshRenderBrusher.T4MPreview.orthographicSize * 0.9f);
				}
				else if (MeshRenderBrusher.PaintPrev != MeshRenderBrusher.PaintHandle.Classic &&
				         MeshRenderBrusher.PaintPrev != MeshRenderBrusher.PaintHandle.Hide_preview &&
				         MeshRenderBrusher.PaintPrev != MeshRenderBrusher.PaintHandle.Follow_Normal_Circle)
				{
					Handles.color = new Color(1f, 1f, 0f, 1f);
					Handles.DrawWireDisc(raycastHit.point, raycastHit.normal,
						MeshRenderBrusher.T4MPreview.orthographicSize * 0.9f);
				}
				//这里需要个预览圈圈，是不是mask处理的？
				//先用一个黄圈代替
				else if (MeshRenderBrusher.PaintPrev == MeshRenderBrusher.PaintHandle.Classic
				         || MeshRenderBrusher.PaintPrev == MeshRenderBrusher.PaintHandle.Hide_preview)
				{
					Handles.color = new Color(1f, 1f, 0f, 0.05f);
					Handles.DrawSolidDisc(raycastHit.point, raycastHit.normal,
						MeshRenderBrusher.T4MPreview.orthographicSize * 0.9f);
				}

				if ((e.type == EventType.MouseDrag && e.alt == false && e.shift == false && e.button == 0) ||
				    (e.shift == false && e.alt == false && e.button == 0 && ToggleF == false))
				{
					Vector2 pixelUV = new Vector2(raycastHit.point.x - raycastHit.transform.position.x,
						                  raycastHit.point.z - raycastHit.transform.position.z)
					                  * MeshRenderBrusher.T4MMaskTexUVCoord //; //0.14f;
										* (MeshRenderBrusher.T4MMaskBlendTex.width / raycastHit.transform.GetComponent<BoxCollider>().size.x);
					int PuX = Mathf.FloorToInt(pixelUV.x);// * MeshRenderBrusher.T4MMaskTex.width);
					int PuY = Mathf.FloorToInt(pixelUV.y);// * MeshRenderBrusher.T4MMaskTex.height);
					int x = Mathf.Clamp(PuX - MeshRenderBrusher.T4MBrushSizeInPourcent / 2, 0, MeshRenderBrusher.T4MMaskBlendTex.width - 1);
					int y = Mathf.Clamp(PuY - MeshRenderBrusher.T4MBrushSizeInPourcent / 2, 0, MeshRenderBrusher.T4MMaskBlendTex.height - 1);
					int width = Mathf.Clamp((PuX + MeshRenderBrusher.T4MBrushSizeInPourcent / 2), 0, MeshRenderBrusher.T4MMaskBlendTex.width - 1) - x + 1;
					int height = Mathf.Clamp((PuY + MeshRenderBrusher.T4MBrushSizeInPourcent / 2), 0, MeshRenderBrusher.T4MMaskBlendTex.height - 1) - y + 1;
					
					//Debug.LogError($"{x}, {y}, {width}, {height}");
					
					
					//决定先尝试，处理完后整体刷新，看看效率
					mBounderCal.Refresh(x, y, width, height, MeshRenderBrusher.T4MMaskBlendTex.width - 1);
					
					//blend和index的rgb值。。。分别是前3强的SourceTex
					MRBPixelModel.TransferFromColor(
						MeshRenderBrusher.T4MMaskIndexTex.GetPixels(mBounderCal.CalStartX,
							mBounderCal.CalStartY, mBounderCal.CalWidth, mBounderCal.CalHeight, 0),
						MeshRenderBrusher.T4MMaskBlendTex.GetPixels(mBounderCal.CalStartX,
							mBounderCal.CalStartY, mBounderCal.CalWidth, mBounderCal.CalHeight, 0), mPixelList);

					if (MeshRenderBrusher.PaintPrev == MeshRenderBrusher.PaintHandle.Hide_preview)
					{
						Debug.LogError("---------------Before Cal------------------------");
						for (int i = 0; i < mBounderCal.CalWidth * mBounderCal.CalHeight; i++)
						{
							Debug.LogError($"{i} : {mPixelList[i]}");
						}
					}


					int offStartX = mBounderCal.OffStartX;
					int offStartY = mBounderCal.OffStartY;
					int offEndX = mBounderCal.OffEndX;
					int offEndY = mBounderCal.OffEndY;
					
					for (int i = 0 - offStartY; i < height - offEndY; i++)
					{
						for (int j = 0 - offStartX; j < width - offEndX; j++)
						{
							int index = ((i + offStartY) * (width - offEndX + offStartX)) + (j + offStartX);
							
							
							mPixelList[index].RefreshCalculateData();

							if (i < 0 || j < 0 || i >= height || j >= width) 
							{
								continue;
							}

							float Stronger = MeshRenderBrusher.T4MBrushAlpha[
								                 Mathf.Clamp(
									                 (y + i) - (PuY - MeshRenderBrusher.T4MBrushSizeInPourcent / 2),
									                 0, MeshRenderBrusher.T4MBrushSizeInPourcent - 1)
								                 * MeshRenderBrusher.T4MBrushSizeInPourcent
								                 +
								                 Mathf.Clamp(
									                 (x + j) - (PuX - MeshRenderBrusher.T4MBrushSizeInPourcent / 2),
									                 0, MeshRenderBrusher.T4MBrushSizeInPourcent - 1)]
							                 * MeshRenderBrusher.T4MStronger;

							//省点运算
							if (Stronger == 0) continue;

							//todo:这里有问题
							//index里面r:0.35,g:0.33,b:0.32,那新点的颜色强度只能超过0.32，才能染上颜色
							//之后再考虑算法优化吧
							//暂时用Stronger来处理了。感觉还能用
							mPixelList[index].CalculateIndex(MeshRenderBrusher.T4MselTexture, Stronger);
							mPixelList[index].RefreshSelfPixelIndex();
						}
					}

					//优化核心算法，根据Index重新排序
					MRBPixelModel.ResortListAccordingToIndex(mPixelList, mBounderCal);
					
					if (MeshRenderBrusher.PaintPrev == MeshRenderBrusher.PaintHandle.Hide_preview)
					{
						Debug.LogError("------------------After Cal-----------------------");
						for (int i = 0; i < mBounderCal.CalWidth * mBounderCal.CalHeight; i++)
						{
							Debug.LogError($"{i} : {mPixelList[i]}");
						}
						ToggleF = true;
						return;
					}
					
					MeshRenderBrusher.T4MMaskBlendTex.SetPixels(x, y, width, height, MRBPixelModel.TransferToColorArray(mPixelList, mBounderCal,true), 0);
					
					
					MeshRenderBrusher.T4MMaskBlendTex.Apply();
					if (MeshRenderBrusher.T4MMaskIndexTex)
					{
						MeshRenderBrusher.T4MMaskIndexTex.SetPixels(x, y, width, height, MRBPixelModel.TransferToColorArray(mPixelList, mBounderCal,false), 0);

						MeshRenderBrusher.T4MMaskIndexTex.Apply();
						UndoObj = new Texture2D[2];
						UndoObj[0] = MeshRenderBrusher.T4MMaskBlendTex;
						UndoObj[1] = MeshRenderBrusher.T4MMaskIndexTex;
					}
					else
					{
						UndoObj = new Texture2D[1];
						UndoObj[0] = MeshRenderBrusher.T4MMaskBlendTex;
					}

					Undo.RegisterUndo(UndoObj, "T4MMask");

					ToggleF = true;

				}
				else if (e.type == EventType.MouseUp && e.alt == false && e.button == 0 && ToggleF == true)
				{
					MeshRenderBrusher.SaveTexture();
					ToggleF = false;
				}
            }
        }
    }

    public static void RefreshTextures()
    {
	    int width = MeshRenderBrusher.T4MMaskBlendTex.width - 1;
	    int height = MeshRenderBrusher.T4MMaskBlendTex.height - 1;
	    
	    mBounderCal.Refresh(1, 1, MeshRenderBrusher.T4MMaskBlendTex.width - 1,
		    MeshRenderBrusher.T4MMaskBlendTex.height - 1, MeshRenderBrusher.T4MMaskBlendTex.width - 1);
	    
	    //读取，是对的
	    MRBPixelModel.TransferFromColor(
		    MeshRenderBrusher.T4MMaskIndexTex.GetPixels(mBounderCal.CalStartX,
			    mBounderCal.CalStartY, mBounderCal.CalWidth, mBounderCal.CalHeight, 0),
		    MeshRenderBrusher.T4MMaskBlendTex.GetPixels(mBounderCal.CalStartX,
			    mBounderCal.CalStartY, mBounderCal.CalWidth, mBounderCal.CalHeight, 0), mPixelList);
	    int offStartX = mBounderCal.OffStartX;
	    int offStartY = mBounderCal.OffStartY;
	    int offEndX = mBounderCal.OffEndX;
	    int offEndY = mBounderCal.OffEndY;

	    //循环，好像也没啥问题
	    for (int i = 0 - offStartY; i < height - offEndY; i++)
	    {
		    for (int j = 0 - offStartX; j < width - offEndX; j++)
		    {
			    int index = ((i + offStartY) * (width - offEndX + offStartX)) + (j + offStartX);
			    mPixelList[index].RefreshCalculateData();
			    mPixelList[index].SortIndexAndWeightModelList();
			    mPixelList[index].RefreshSelfPixelIndex();
			    if (i < 0 || j < 0 || i >= height || j >= width) 
			    {
				    continue;
			    }
			    //这个没弄成private set。。。没啥问题吧？
			    mPixelList[index].IsDirty = true;
		    }
	    }
	    //这个，有问题，这里是刷新，不需要Recommend
	    //不用Recommend会变得更好嘛？因为数据会变得更简洁？但是可能出现更明显的锯齿？？
	    //试一下没有Recommend吧
	    MRBPixelModel.ResortListAccordingToIndex(mPixelList, mBounderCal);
	    
	    
	    //整理赋值，是对的
	    mBounderCal.Refresh(0, 0, MeshRenderBrusher.T4MMaskBlendTex.width,
		    MeshRenderBrusher.T4MMaskBlendTex.height, MeshRenderBrusher.T4MMaskBlendTex.width - 1);
	    
	    MeshRenderBrusher.T4MMaskBlendTex.SetPixels(0, 0, MeshRenderBrusher.T4MMaskBlendTex.width,
		    MeshRenderBrusher.T4MMaskBlendTex.height, MRBPixelModel.TransferToColorArray(mPixelList, mBounderCal, true),
		    0);
	    MeshRenderBrusher.T4MMaskBlendTex.Apply();
	    
	    MeshRenderBrusher.T4MMaskIndexTex.SetPixels(0, 0, MeshRenderBrusher.T4MMaskBlendTex.width,
		    MeshRenderBrusher.T4MMaskBlendTex.height,
		    MRBPixelModel.TransferToColorArray(mPixelList, mBounderCal, false), 0);
	    MeshRenderBrusher.T4MMaskIndexTex.Apply();
    }
}
