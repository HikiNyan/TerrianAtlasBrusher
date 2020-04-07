/*
 *
 * 优化渲染管道，所以弄个Model来处理Pixel的rgb。
 * 不再像以前一样按照Weight来排序
 * 之前用Color[]来处理像素点真的。。。。勉强够用，现在就，需要个class来处理了。。。
 * 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MRBPixelModel
{
	/*private int mPosX;
	public int PosX { get { return mPosX; }}
	
	private int mPosY;
	public int PosY { get { return mPosY; }}*/
	
	//如果Index的排序做了改变，就置Ditry，然后需要刷新
	//不用weight改变，省计算
	private bool mIsDirty = false;
	public bool IsDirty
	{
		get { return mIsDirty; }
		set { mIsDirty = value; }
	}

	//想把这个Color改成一个新Model，里面用float数组存参数(按Index排序的时候很多下标相关取值)
	//rgb也可以写get，set方法，对应float数组中参数
	private Color mSelfPixelIndex;
	public Color SelfPixelIndex{ get { return mSelfPixelIndex; }}
	
	private Color mSelfPixelBlend;
	public Color SelfPixelBlend { get { return mSelfPixelBlend; }}

	//把本来在Extends里面的List挪到Model里面，自己计算
	//感觉名字起的有点乱。。。这个肯定是自己的，用于计算
	private List<TexIndexAndWeightModel> mIndexAndWeightModelList = new List<TexIndexAndWeightModel>();
	private bool mIsModelListInited = false;
	public MRBPixelModel()
	{
	}

	public override string ToString()
	{
		return $"SelfPixelIndex:{SelfPixelIndex.r}, {SelfPixelIndex.g}, {SelfPixelIndex.b}\n" +
		       $"SelfPixelBlend:{SelfPixelBlend.r}, {SelfPixelBlend.g}, {SelfPixelBlend.b}";
	}

	public void RefreshSelfPixelModel(Color colorIndex, Color colorBlend)
	{
		RefreshSelfPixelIndex(colorIndex);
		RefreshSelfPixelBlend(colorBlend);
	}

	private void RefreshSelfPixelIndex(Color colorIndex)
	{
		mSelfPixelIndex = colorIndex;
	}

	private void RefreshSelfPixelBlend(Color colorBlend)
	{
		mSelfPixelBlend = colorBlend;
	}

	/*private void RefreshSelfPosition(int posX, int posY)
	{
		mPosX = posX;
		mPosY = posY;
	}*/

	#region Resort

	//分别对应r,g,b,a/empty存的参数
	private int[] mHelpCalPos = new int[4];
	private bool isShowLog = false;
	private void ResortByIndexResult(int[] resultArray)
	{
		if (resultArray.Length != 3 && resultArray[0] + resultArray[1] + resultArray[2] != 3) return;
		if (isShowLog)
		{
			//Debug.LogError("------------------Before Resort--------------------");
			Debug.LogError(ToString());
		}
		
		//反正length肯定是3
		//不new Color，就叫换原Color数值，继续省
		//思路
		//穷举？？？好像省运算多写代码没问题，反正情况不多
		//然后，真的穷举，但是代码也不难看
		ResetHelpCalPos();
		for (int i = 0; i < resultArray.Length; i++)
		{
			//目标位置是自己，不处理
			if (resultArray[i] == i)
			{
				//如果自己位置是空，则填补
				if (mHelpCalPos[3] == i)
				{
					SetValue(mHelpCalPos[i], mHelpCalPos[3]);
				}
			}

			//目标位置为空，直接赋值
			if (resultArray[i] == mHelpCalPos[3])
			{
				SetValue(mHelpCalPos[i], mHelpCalPos[3]);
				//把HelpCalPos的处理也放到SetValue里面
				/*mHelpCalPos[3] = i;
				mHelpCalPos[i] = resultArray[i];*/
				continue;
			}
			
			//否则需要操作
			//1.目标->Empty(这时目标和Empty会互换)
			//2.自己->Empty
			SetValue(resultArray[i], mHelpCalPos[3]);
			SetValue(mHelpCalPos[i], mHelpCalPos[3]);
		}

		if (isShowLog)
		{
			//Debug.LogError("------------------After Resort--------------------");
			Debug.LogError(ToString());
			Debug.LogError("--------------------------------------");

		}
	}

	private void ResetHelpCalPos()
	{
		for (int i = 0; i < mHelpCalPos.Length; i++)
		{
			mHelpCalPos[i] = i;
		}
	}

	private void SetValue(int from, int to)
	{
		if (from == to) return;
		
		float tempIndexValue, tempBlendValue;
		tempIndexValue = GetColorValueByInt(mSelfPixelIndex, from);
		tempBlendValue = GetColorValueByInt(mSelfPixelBlend, from);
		SetColorValueByInt(tempIndexValue, to, false);
		SetColorValueByInt(tempBlendValue, to, true);
		
		//处理Pos记录
		mHelpCalPos[from] = to;
		mHelpCalPos[3] = from;
	}

	private float GetColorValueByInt(Color targetColor, int index)
	{
		if (index == 0)
		{
			return targetColor.r;
		}
		if (index == 1)
		{
			return targetColor.g;
		}
		if (index == 2)
		{
			return targetColor.b;
		}
		return targetColor.a;
	}

	private void SetColorValueByInt(float value, int index, bool isBlend = true)
	{
		//这里没有return，所以用else if，好烦
		if (isBlend)
		{
			if (index == 0)
			{
				mSelfPixelBlend.r = value;
			}
			else if (index == 1)
			{
				mSelfPixelBlend.g = value;
			}
			else if (index == 2)
			{
				mSelfPixelBlend.b = value;
			}
			else
			{
				mSelfPixelBlend.a = value;
			}
		}
		else
		{
			if (index == 0)
			{
				mSelfPixelIndex.r = value;
			}
			else if (index == 1)
			{
				mSelfPixelIndex.g = value;
			}
			else if (index == 2)
			{
				mSelfPixelIndex.b = value;
			}
			else
			{
				mSelfPixelIndex.a = value;
			}
		}
	}

	#endregion

	#region ModelList

	#region 刷新+顺便初始化

	/// <summary>
	/// 刷新+顺便初始化
	/// </summary>
	public void RefreshCalculateData()
	{
		if (null == mIndexAndWeightModelList || mIndexAndWeightModelList.Count < 3)
		{
			InitModelList();
		}
		mIndexAndWeightModelList[0].SetValue(mSelfPixelIndex.r, mSelfPixelBlend.r);
		mIndexAndWeightModelList[1].SetValue(mSelfPixelIndex.g, mSelfPixelBlend.g);
		mIndexAndWeightModelList[2].SetValue(mSelfPixelIndex.b, mSelfPixelBlend.b);
		mIndexAndWeightModelList[3].Reset();
	}
	
	void InitModelList()
	{
		if (null == mIndexAndWeightModelList)
		{
			mIndexAndWeightModelList = new List<TexIndexAndWeightModel>();
		}

		for (int i = 0; i < 4; i++)
		{
			if (mIndexAndWeightModelList.Count <= i)
			{
				mIndexAndWeightModelList.Add(new TexIndexAndWeightModel(MeshRenderBrusher.MaxTexNum));
			}

			mIndexAndWeightModelList[i].Reset();
		}

		mIsModelListInited = true;
	}

	#endregion --刷新+顺便初始化

	#region 重新计算数值，顺便置Dirty

	/// <summary>
	/// 重新计算数值，顺便置Dirty
	/// </summary>
	/// <param name="index"></param>
	/// <param name="curWeight"></param>
	public void CalculateIndex(int index, float curWeight)
	{
		if (IsHavePreSource(index))
		{
			for (int i = 0; i < mIndexAndWeightModelList.Count; i++)
			{
				if (null != mIndexAndWeightModelList[i] && index == mIndexAndWeightModelList[i].TexIndex)
				{
					mIndexAndWeightModelList[i].AddWeight(1, curWeight);
				}
				else
				{
					mIndexAndWeightModelList[i].AddWeight(0, curWeight);
				}
			}
		}
		else
		{
			if (IsNeedReplace(curWeight))
			{
				mIndexAndWeightModelList[mIndexAndWeightModelList.Count - 1].SetValue(index, curWeight);
				for (int i = 0; i < mIndexAndWeightModelList.Count; i++)
				{
					if (null != mIndexAndWeightModelList[i] && index != mIndexAndWeightModelList[i].TexIndex)
					{
						mIndexAndWeightModelList[i].AddWeight(0, curWeight);
					}
				}
			}
		}
		mIsDirty = CheckSetDirty();
		//为什么全dirty一起刷一遍问题就更大了？sort index里面的算法有bug？
		//mIsDirty = true;
		mIndexAndWeightModelList.Sort();
	}

	private bool IsHavePreSource(int index)
	{
		//最后一个留在Replace时候使用
		for (int i = 0; i < mIndexAndWeightModelList.Count - 1; i++)
		{
			if (null != mIndexAndWeightModelList[i] && index == mIndexAndWeightModelList[i].TexIndex)
			{
				return true;
			}
		}
	    
		return false;
	}

	/// <summary>
	/// 在IsHavePreSource为false的情况下再判断
	/// </summary>
	/// <returns></returns>
	private bool IsNeedReplace(float weight)
	{
		//最后一个留在Replace时候使用
		for (int i = 0; i < mIndexAndWeightModelList.Count - 1; i++)
		{
			if (null != mIndexAndWeightModelList[i] && weight > mIndexAndWeightModelList[i].TexWeight)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// 根据weight的大小，判断是否是dirty
	/// 然后正常排序，在写Tex之前会重新刷List，来修改排序
	/// </summary>
	/// <returns></returns>
	private bool CheckSetDirty()
	{
		for (int i = 0; i < mIndexAndWeightModelList.Count - 1; i++)
		{
			if (mIndexAndWeightModelList[i].TexWeight < mIndexAndWeightModelList[i + 1].TexWeight)
			{
				return true;
			}
		}

		return false;
	}

	#endregion --重新计算数值，顺便置Dirty

	#region 根据mIndexAndWeightModelList，刷新Color中数值

	public void RefreshSelfPixelIndex()
	{
		if (!mIsModelListInited)
		{
			return;
		}

		//这里还是清一下零，不过在IndexSort的时候，会处理0数据，把index重新赋值
		//避免出现，Near参数有2个0，然后index不平滑的情况
		//主要是，这里获取不到Near的数据(本来就没想在这里处理的)。所以，只能放到下面
		//不然真的想在这里处理完Self的Index状态
		ResetColorValueByWeight();

		var blendColor = new Color(mIndexAndWeightModelList[0].TexWeight,
			mIndexAndWeightModelList[1].TexWeight, mIndexAndWeightModelList[2].TexWeight);
		
		var indexColor =  new Color(mIndexAndWeightModelList[0].ColorTexIndex,
			mIndexAndWeightModelList[1].ColorTexIndex, mIndexAndWeightModelList[2].ColorTexIndex);
		
		RefreshSelfPixelModel(indexColor, blendColor);
	}

	private void ResetColorValueByWeight()
	{
		//0.001是随便设定的值。。。大概，千分之一的效果，存在也看不出来了
		//0.001不行啊，得0.01，为了效果，试试看
		if (mIndexAndWeightModelList[2].TexWeight < 0.01f)
		{
			mIndexAndWeightModelList[2].Reset();
			//已经排完序的，所以算b,g
			if (mIndexAndWeightModelList[1].TexWeight < 0.01f)
			{
				mIndexAndWeightModelList[1].Reset();
			}
		}
	}

	public void SortIndexAndWeightModelList()
	{
		mIndexAndWeightModelList.Sort();
	}



	#endregion --根据mIndexAndWeightModelList，刷新Color中数值
	
	#endregion --ModelList

	#region static方法

	public static void TransferFromColor(Color[] colorIndexArray, Color[] colorBlendArray,
		List<MRBPixelModel> targetList)
	{
		for (int i = 0; i < colorIndexArray.Length; i++)
		{
			while (targetList.Count <= i)
			{
				targetList.Add(new MRBPixelModel());
			}

			TransferFromColor(colorIndexArray[i], colorBlendArray[i], targetList[i]);
		}
	}

	public static void TransferFromColor(Color colorIndex, Color colorBlend, MRBPixelModel target)
	{
		target.RefreshSelfPixelModel(colorIndex, colorBlend);
		target.IsDirty = false;
	}

	public static Color[] TransferToColorArray(List<MRBPixelModel> targetList, TexAreaCalWithBounder bounderCal, bool isBlend)
	{
		//基本算是冗余的判断
		int count = bounderCal.OriWidth * bounderCal.OriHeight;
		if (count <= 0) return null;
		
		Color[] colorArray = new Color[count];
		int arrayIndex, listIndex;
		int offX = bounderCal.OffStartX;
		int offY = bounderCal.OffStartY;
		
		for (int i = 0; i < bounderCal.OriWidth; i++)
		{
			for (int j = 0; j < bounderCal.OriHeight; j++)
			{
				arrayIndex = j * bounderCal.OriWidth + i;
				listIndex =  (j + offY) * bounderCal.CalWidth + (i + offX);

				if (arrayIndex >= colorArray.Length || arrayIndex >= targetList.Count)
				{
					Debug.LogError($"i : {i}, j : {j}, arrayIndex : {arrayIndex}");
				}

				if (isBlend)
				{
					colorArray[arrayIndex] = targetList[listIndex].SelfPixelBlend;
				}
				else
				{
					colorArray[arrayIndex] = targetList[listIndex].SelfPixelIndex;
				}
			}
		}
		return colorArray;
	}

	#region 按Index排序

	//脏像素附近的干净像素的index
	private static List<int> mNearIndexList = new List<int>();
	
	//用来把置为0的Index刷成推荐的Index，只取前三
	//对了，这里还在计算是顺便担任了临时记录self中，已有的index的功能
	private static float[] mRecommendIndexResultArray = new float[2];
	//记录near所有可能的Index及其weight(省new的开销)，用来给result赋值
	private static List<TexIndexAndWeightModel> mRecommendIndexNearList = new List<TexIndexAndWeightModel>();

	
	//V1:存储干净像素对应Index的权重的数组
	//private static float[] mIndexWeightArray = new float[3];
	//V2:
	private static List<IndexSortingModel> mIndexSortingWeightList = new List<IndexSortingModel>();
	//存储计算结果的数组
	private static int[] mIndexResultArray = new int[3];
	
	/// <summary>
	/// 这里不考虑，刷一下的范围，触碰>=3条边的深井冰情况
	/// </summary>
	/// <param name="targetList"></param>
	/// <param name="bounderCal"></param>
	public static void ResortListAccordingToIndex(List<MRBPixelModel> targetList, TexAreaCalWithBounder bounderCal)
	{
		int count = 0;
		int calCount = bounderCal.CalWidth * bounderCal.CalHeight;
		for (int i = 0; i < calCount; i++)
		{
			if (targetList[i].IsDirty) count++;
		}

		int curIndex = 0;
		
		
		//理论上，都会在一轮循环以后处理完毕
		//当然得配合上边角的优化计算。对index做个处理应该就好了
		//然后还得考虑边角的非dirty
		//考虑过使用List记录Index的情况，但是考虑到比较极限的情况循环次数(计算周边是否Dirty的次数)可能会非常大。
		//索性通过算法来尽量一次循环。
		//说好的不考虑消耗呢！

		int loopCount = 0;
		while (count > 0 && loopCount < 5)
		{
			for (int i = 0; i < bounderCal.OriWidth; i++)
			{
				if (count == 0) break;
				
				for (int j = 0; j < bounderCal.OriHeight; j++)
				{
					if (count == 0) break;
					
					curIndex = CalCurIndex(i, j, bounderCal);
					if (!targetList[curIndex].IsDirty)
					{
						continue;
					}
					//考虑把bounderCal用static代替，但是他在另一个脚本里面，有点不放心
					//先只计算4个相邻的格子，看效果，有必要再+4个斜对角格子，加权计算
					CalCurNearIndex(targetList, curIndex, bounderCal);
					//周边只有0-1个干净的像素，先不计算(理论上不可能出现这种情况)
					if (mNearIndexList.Count < 2)
					{
						continue;
					}

					
					ResetCurIndexWithRecommendIndex(targetList, curIndex);
					
					//大致思路
					//1.脏像素一定是排好序的，所以以Index的r,g的顺序来确定权重(b最后放剩下的)
					//2.关于权重存储，预定是给个int[3]，来分别处理干净像素的r,g,b对应的权重值
					//3.关于权重值，如果有相同,且数值>0.01f，则+1，如果不相同且weight<0.01f，则+0.5(暂时，看情况优化)
					//4.然后根据权重值sort
					//TODO:如果这么计算权重中，3个值都为0，还可以把4个对角的格子加入权重计算，暂时不添加
					CalIndexWeight(targetList, curIndex);
					//计算完毕，用mIndexResultArray开始调整位置吧
					targetList[curIndex].ResortByIndexResult(mIndexResultArray);
					targetList[curIndex].IsDirty = false;
					count--;
				}
			}
			loopCount++;
		}

		for (int i = 0; i < calCount; i++)
		{
			//Debug.LogError($"{i}\n{targetList[i].ToString()}");
		}

		if (count > 0)
		{
			Debug.LogError("出现意外错误，请先确认刷新范围是否触碰>=3条边，否则去找程序处理");
		}
	}

	#region 都是算计

	private static int CalCurIndex(int i, int j, TexAreaCalWithBounder bounderCal)
	{
		int calPiexlIndexX = 0;
		int calPiexlIndexY = 0;
		
		//左侧贴边，从右侧开始算
		if (bounderCal.IsBounder[0])
		{
			calPiexlIndexX = bounderCal.OriWidth - (i + bounderCal.OffStartX) - 1;
		}
		else
		{
			calPiexlIndexX = (i + bounderCal.OffStartX);
		}

		if (bounderCal.IsBounder[1])
		{
			calPiexlIndexY = (bounderCal.OriHeight - (j + bounderCal.OffStartY) - 1) * bounderCal.CalWidth;
		}
		else
		{
			calPiexlIndexY = (j + bounderCal.OffStartY) * bounderCal.CalWidth;
		}

		return calPiexlIndexX + calPiexlIndexY;
	}

	private static void CalCurNearIndex(List<MRBPixelModel> targetList, int curIndex, TexAreaCalWithBounder bounderCal)
	{
		mNearIndexList.Clear();
		int calIndex = 0;
		//四个边
		if (!bounderCal.IsBounder[0])
		{
			calIndex = curIndex - 1;
			AddCurNearIndexToList(calIndex, targetList[calIndex]);
		}

		if (!bounderCal.IsBounder[1])
		{
			calIndex = curIndex - bounderCal.CalWidth;
			AddCurNearIndexToList(calIndex, targetList[calIndex]);
		}
		if (!bounderCal.IsBounder[2])
		{
			calIndex = curIndex + 1;
			AddCurNearIndexToList(calIndex, targetList[calIndex]);
		}
		if (!bounderCal.IsBounder[3])
		{
			calIndex = curIndex + bounderCal.CalWidth;
			AddCurNearIndexToList(calIndex, targetList[calIndex]);
		}
		/*//4个边感觉数量不够，再+4个角试试看
		//左下
		if (!bounderCal.IsBounder[0] && !bounderCal.IsBounder[1])
		{
			calIndex = curIndex - 1 - bounderCal.CalWidth;
			AddCurNearIndexToList(calIndex, targetList[calIndex]);
		}
		//左上
		if (!bounderCal.IsBounder[0] && !bounderCal.IsBounder[3])
		{
			calIndex = curIndex - 1 + bounderCal.CalWidth;
			AddCurNearIndexToList(calIndex, targetList[calIndex]);
		}
		//右下
		if (!bounderCal.IsBounder[2] && !bounderCal.IsBounder[1])
		{
			calIndex = curIndex + 1 - bounderCal.CalWidth;
			AddCurNearIndexToList(calIndex, targetList[calIndex]);
		}
		//右上
		if (!bounderCal.IsBounder[2] && !bounderCal.IsBounder[3])
		{
			calIndex = curIndex + 1 + bounderCal.CalWidth;
			AddCurNearIndexToList(calIndex, targetList[calIndex]);
		}*/
	}

	private static void AddCurNearIndexToList(int calIndex, MRBPixelModel model)
	{
		if (!model.IsDirty)
		{
			mNearIndexList.Add(calIndex);
		}
	}

	//避免出现，Near参数有2个0，然后index不平滑的情况
	//处理方法：先根据Slef中Weight为0的数量，取Near最优先的0-2个Index，然后随意赋值，然后再跑排序
	//突然想到，可能，一片都是一个颜色，那get的index值还是0。。不过好像没什么关系哦。。。
	private static void ResetCurIndexWithRecommendIndex(List<MRBPixelModel> targetList, int curIndex)
	{
		int needIndexCount = 0;
		if (targetList[curIndex].SelfPixelIndex.r < 0.01f && targetList[curIndex].SelfPixelBlend.r < 0.001f)
		{
			Debug.LogError($"按照Weight的排序有问题，r值也为0。curIndex : {curIndex}");
		}
		else if(targetList[curIndex].SelfPixelIndex.g < 0.01f && targetList[curIndex].SelfPixelBlend.g < 0.001f)
		{
			needIndexCount = 2;
			ResetRecommendIndexResultArray(targetList[curIndex].SelfPixelIndex.r);
		}
		else if(targetList[curIndex].SelfPixelIndex.b < 0.01f && targetList[curIndex].SelfPixelBlend.b < 0.001f)
		{
			needIndexCount = 1;
			ResetRecommendIndexResultArray(targetList[curIndex].SelfPixelIndex.r, targetList[curIndex].SelfPixelIndex.g);
		}

		//算是，省计算吧
		if (needIndexCount == 0) return;

		SetRecommendIndexResult(targetList, curIndex);
		if (needIndexCount == 2)
		{
			//因为写在一个类里面所以可以用private的参数可，太过分了
			//这里偷个懒，直接用吧
			targetList[curIndex].mSelfPixelIndex.g = mRecommendIndexResultArray[0];
			targetList[curIndex].mSelfPixelIndex.b = mRecommendIndexResultArray[1];
		}
		else if (needIndexCount == 1)
		{
			targetList[curIndex].mSelfPixelIndex.b = mRecommendIndexResultArray[0];
		}
	}

	private static void ResetRecommendIndexResultArray(float firstValue = -1, float secondValue = -1)
	{
		mRecommendIndexResultArray[0] = firstValue;
		mRecommendIndexResultArray[1] = secondValue;
	}

	//不管需要几个，都返回2个(预定)推荐的Index
	//而且mNearIndexList已经计算完毕，不修改就行了
	private static void SetRecommendIndexResult(List<MRBPixelModel> targetList, int curIndex)
	{
		for (int i = mRecommendIndexNearList.Count; i < 2; i++)
		{
			mRecommendIndexNearList.Add(new TexIndexAndWeightModel(MeshRenderBrusher.MaxTexNum));
		}

		for (int i = 0; i < mRecommendIndexNearList.Count; i++)
		{
			mRecommendIndexNearList[i].Reset();
		}
		
		//记得要剔除self中已有的Index。。。
		for (int i = 0; i < mNearIndexList.Count; i++)
		{
			AddRecommendIndexNearList(targetList[mNearIndexList[i]].SelfPixelIndex.r,
				targetList[mNearIndexList[i]].SelfPixelBlend.r);
			AddRecommendIndexNearList(targetList[mNearIndexList[i]].SelfPixelIndex.g,
				targetList[mNearIndexList[i]].SelfPixelBlend.g);
			AddRecommendIndexNearList(targetList[mNearIndexList[i]].SelfPixelIndex.b,
				targetList[mNearIndexList[i]].SelfPixelBlend.b);
		}
		mRecommendIndexNearList.Sort();

		//上面确保了，至少有List里面至少有两个
		//mRecommendIndexResultArray 这个从临时记录的排除重复Index，变成了真的Result
		mRecommendIndexResultArray[0] = mRecommendIndexNearList[0].ColorTexIndex >= -0.01f ? mRecommendIndexNearList[0].ColorTexIndex : 0;
		mRecommendIndexResultArray[1] = mRecommendIndexNearList[1].ColorTexIndex >= -0.01f ? mRecommendIndexNearList[1].ColorTexIndex : 0;
	}

	private static void AddRecommendIndexNearList(float index, float weight)
	{
		if (Mathf.Abs(index - mRecommendIndexResultArray[0]) < 0.01f
		    || Mathf.Abs(index - mRecommendIndexResultArray[1]) < 0.01f)
		{
			return;
		}

		//就算weight==0，也要计算，不然的话会出问题，所以给赋一个默认最小值，只要有这个index的话
		//做到这里才发现，需要一个空的标志位。就很烦，要改改一大片
		if (index > 0.01f)
		{
			weight += 0.0011f;
		}
		else
		{
			weight += 0.00101f;
		}

		bool isIndexCal = false;
		
		for (int i = 0; i < mRecommendIndexNearList.Count; i++)
		{
			//有记录，直接+
			if (Mathf.Abs(index - mRecommendIndexNearList[i].ColorTexIndex) < 0.01f)
			{
				mRecommendIndexNearList[i].SimpleAddWeight(weight);
				isIndexCal = true;
				break;
			}
			//没记录，List够长
			if (mRecommendIndexNearList[i].ColorTexIndex < -0.01f)
			{
				mRecommendIndexNearList[i].SetValue(index, weight);
				isIndexCal = true;
				break;
			}
		}

		if (!isIndexCal)
		{
			mRecommendIndexNearList.Add(new TexIndexAndWeightModel(MeshRenderBrusher.MaxTexNum, index, weight));
		}
	}




	private static void CalIndexWeight(List<MRBPixelModel> targetList, int curIndex)
	{
		//反正Length是3
		for (int i = 0; i  < mIndexResultArray.Length; i ++)
		{
			mIndexResultArray[i] = -1;
		}

		//万一没有呢！！！r,g没有对应的值，占了0,1的位置，但是b值有对应的值，然后，崩了
		//那这样，这里的算法就有问题了
		//正确算法应该是，计算r,g,b三个值的，分别对应0,1,2，三个位置的weight，然后排序
		//这个就需要记录所有的rgb计算后的结果，然后比对。
		//需要，在写个Model嘛，毕竟用Dictionary<int, List<TexIndexAndWeightModel>>不太好排序
		//就，写个继承TexIndexAndWeightModel的新类就好了
		
		//V1
		/*AddColorIndexWeight(targetList, 0, targetList[curIndex].SelfPixelIndex.r);
		AddColorIndexWeight(targetList, 1, targetList[curIndex].SelfPixelIndex.g);
		//机智的小运算
		mIndexResultArray[2] = 3 - mIndexResultArray[0] - mIndexResultArray[1];*/
		
		//V2
		CalIndexSortingWeightList(targetList, curIndex);
		SetIndexResultArray();
		
	}

	#region CalIndexSortingV1

	/*//计算单种颜色(r/g/b)的Index
	//index就0和1两种情况，所以。。。。下面mIndexResultArray的处理有部分写死
	private static void AddColorIndexWeight(List<MRBPixelModel> targetList, int index, float selfIndexValue)
	{
		float tempMaxValue = -1;

		ClearIndexWeightArray();
		//Q:发现，为了省消耗，好像用了很多static，也挺不安全的
		for (int i = 0; i < mNearIndexList.Count; i++)
		{
			AddSingleIndexWeight(selfIndexValue, 0, targetList[mNearIndexList[i]].SelfPixelIndex.r,
				targetList[mNearIndexList[i]].SelfPixelBlend.r);
			AddSingleIndexWeight(selfIndexValue, 1, targetList[mNearIndexList[i]].SelfPixelIndex.g,
				targetList[mNearIndexList[i]].SelfPixelBlend.g);
			AddSingleIndexWeight(selfIndexValue, 2, targetList[mNearIndexList[i]].SelfPixelIndex.b,
				targetList[mNearIndexList[i]].SelfPixelBlend.b);
		}

		//这里理论上不会没值的
		if (NotSkipPreResult(index, 0))
		{
			tempMaxValue = mIndexWeightArray[0];
			mIndexResultArray[index] = 0;
		}
		if (mIndexWeightArray[1] > tempMaxValue && NotSkipPreResult(index, 1))
		{
			tempMaxValue = mIndexWeightArray[1];
			mIndexResultArray[index] = 1;
		}
		if (mIndexWeightArray[2] > tempMaxValue && NotSkipPreResult(index, 2))
		{
			mIndexResultArray[index] = 2;
		}
	}
	
	private static void ClearIndexWeightArray()
	{
		for (int i = 0; i  < mIndexWeightArray.Length; i ++)
		{
			mIndexWeightArray[i] = 0;
		}
	}

	//是，true就要计算，false就skip掉
	//index只可能是0或者1
	//或者2。。。
	private static bool NotSkipPreResult(int index, int calIndex)
	{
		if (index == 1)
		{
			return mIndexResultArray[0] != calIndex;
		}

		return true;
	}

	//计算单个near像素的对应的值
	private static void AddSingleIndexWeight(float curIndexValue, int index, float targetIndexValue, float targetBlendValue)
	{
		if (Math.Abs(curIndexValue - targetIndexValue) <= 0.01f)
		{
			if (targetBlendValue > 0.001f)
			{
				mIndexWeightArray[index] += 1;
			}
			else
			{
				mIndexWeightArray[index] += 0.8f;
			}

		}
		else if(targetBlendValue < 0.001f)
		{
			mIndexWeightArray[index] += 0.5f;
		}
	}*/

	#endregion

	#region CalIndexSortingV2

	private static void CalIndexSortingWeightList(List<MRBPixelModel> targetList, int curIndex)
	{
		ResetIndexSortingWeightList();
		//Q:得按照写死的3*3来写代码，不然会很麻烦。虽然也可以开放参数，但是暂时没必要
		AddOneGroupIndexSortingWeight(targetList, 0, targetList[curIndex].SelfPixelIndex.r);
		AddOneGroupIndexSortingWeight(targetList, 1, targetList[curIndex].SelfPixelIndex.g);
		AddOneGroupIndexSortingWeight(targetList, 2, targetList[curIndex].SelfPixelIndex.b);
	}

	private static void ResetIndexSortingWeightList()
	{
		//这次真的是固定9个，因为self的rgb，分别对应目标0,1,2的weight
		for (int i = mIndexSortingWeightList.Count; i < 9; i++)
		{
			mIndexSortingWeightList.Add(new IndexSortingModel(i / 3, 3));
		}

		for (int i = 0; i < mIndexSortingWeightList.Count; i++)
		{
			mIndexSortingWeightList[i].SetValueWithModelIndex(i / 3, i % 3, 0);
		}
	}

	private static void AddOneGroupIndexSortingWeight(List<MRBPixelModel> targetList, int baseListIndex, float targetColorIndex)
	{
		for (int i = 0; i < mNearIndexList.Count; i++)
		{
			mIndexSortingWeightList[baseListIndex * 3 + 0].SimpleAddWeight(GetSingleIndexSortingWeight(targetColorIndex,
				targetList[mNearIndexList[i]].SelfPixelIndex.r, targetList[mNearIndexList[i]].SelfPixelBlend.r));
			mIndexSortingWeightList[baseListIndex * 3 + 1].SimpleAddWeight(GetSingleIndexSortingWeight(targetColorIndex,
				targetList[mNearIndexList[i]].SelfPixelIndex.g, targetList[mNearIndexList[i]].SelfPixelBlend.g));
			mIndexSortingWeightList[baseListIndex * 3 + 2].SimpleAddWeight(GetSingleIndexSortingWeight(targetColorIndex,
				targetList[mNearIndexList[i]].SelfPixelIndex.b, targetList[mNearIndexList[i]].SelfPixelBlend.b));
		}
	}

	//类似V1中的AddSingleIndexWeight，不过把存储数值改成返回
	private static float GetSingleIndexSortingWeight(float curIndexValue, float targetIndexValue, float targetBlendValue)
	{
		//添加上blend的比重
		float weight = 0.0001f + targetBlendValue;
		if (Math.Abs(curIndexValue - targetIndexValue) <= 0.01f)
		{
			if (targetBlendValue > 0.001f)
			{
				weight += 1;
			}
			else
			{
				weight += 0.8f;
			}

		}
		else if(targetBlendValue < 0.001f)
		{
			weight += 0.2f;
		}

		return weight;
	}

	private static void SetIndexResultArray()
	{
		mIndexSortingWeightList.Sort();
		int count = 0;
		// && count < mIndexResultArray.Length不做这个判断啊，有问题就报错，反正最多9次循环
		//测完没问题后再考虑+判断省运算
		for (int i = 0; i < mIndexSortingWeightList.Count; i++)
		{
			//除了判断自己有没有位置了，还需要判断该位置有没有被其他index占有
			if (IsSetedIndexResult(mIndexSortingWeightList[i].TexIndex, mIndexSortingWeightList[i].ModelIndex))
			{
				continue;
			}

			mIndexResultArray[mIndexSortingWeightList[i].ModelIndex] = mIndexSortingWeightList[i].TexIndex;
			count++;
		}

		if (count != mIndexResultArray.Length)
		{
			Debug.LogError("排序后不匹配，有问题");
		}
	}

	private static bool IsSetedIndexResult(int targetIndex, int modelIndex)
	{
		if (mIndexResultArray[modelIndex] >= 0) return true;
		for (int i = 0; i < mIndexResultArray.Length; i++)
		{
			if (mIndexResultArray[i] == targetIndex)
			{
				return true;
			}
		}
		return false;
	}

	#endregion

	#endregion --都是算计
	
	#endregion --按Index排序

	#endregion--static方法
}


/// <summary>
/// 刷新比重Model，TerrainExportAtlas写的太惨了，不过别人不需要修改，只需要一次计算，所以简单点没问题
/// </summary>
public class TexIndexAndWeightModel:IComparable<TexIndexAndWeightModel>
{
	//难怪index的值会不太对。。。原来是这里的问题。。。
	//16->9
	private int MaxSourceTexNum = 9;
	public int TexIndex { get; private set; }
	public float ColorTexIndex
	{
		get
		{
			return TexIndex / ((MaxSourceTexNum - 1f));
		}
	}

	public float TexWeight { get; private set; }

	#region Constructor

	private TexIndexAndWeightModel()
	{
	}

	public TexIndexAndWeightModel(int maxTexNum)
	{
		MaxSourceTexNum = maxTexNum;
	}

	private TexIndexAndWeightModel(int index, float weight)
	{
		TexIndex = index;
		TexWeight = weight;
	}

	private TexIndexAndWeightModel(float index, float weight)
	{
		TexIndex = Mathf.RoundToInt(index * (MaxSourceTexNum - 1));
		TexWeight = weight;
	}

	public TexIndexAndWeightModel(int maxTexNum, int index, float weight):this(index, weight)
	{
		MaxSourceTexNum = maxTexNum;
	}
	
	public TexIndexAndWeightModel(int maxTexNum, float index, float weight):this(index, weight)
	{
		MaxSourceTexNum = maxTexNum;
	}

	#endregion

	#region ValueOperate

	public virtual void Reset()
	{
		TexIndex = -1;
		TexWeight = 0;
	}

	public void SetValue(int index, float weight)
	{
		TexIndex = index;
		TexWeight = weight;
	}

	public void SetValue(float index, float weight)
	{
		SetValue(Mathf.RoundToInt(index * (MaxSourceTexNum - 1)), weight);
	}

	//在非color的blend的计算中使用。。。简单直白，不算权重平方
	public void SimpleAddWeight(float addWeight)
	{
		TexWeight += addWeight;
	}

	public void AddWeight(int targetWeight, float addWeight)
	{
		//方案1：直接算(有锯齿)
		//TexWeight += (addWeight / 10);

		//方案2：某范围权重加大(效果不好)
		/*float targetWeight = TexWeight + addWeight / 10;
		if (targetWeight > 0.3f && targetWeight < 0.7f)
		{
			TexWeight += (addWeight / 40);
		}
		else
		{
			TexWeight = targetWeight;
		}*/
		
		//方案3：T4M一样，Lerp
		//这个是对权重算平方？
		TexWeight = TexWeight + (targetWeight - TexWeight) * addWeight;
	}

	public void SetWeight(float addWeight)
	{
		TexWeight = addWeight;
	}

	#endregion
	
	public int CompareTo(TexIndexAndWeightModel other)
	{
		if (ReferenceEquals(this, other)) return 0;
		if (ReferenceEquals(null, other)) return 1;
		return -TexWeight.CompareTo(other.TexWeight);
	}

	/// <summary>
	/// 直接修改传进来的数值
	/// </summary>
	/// <param name="dataList"></param>
	public static void ReCalCulateWeight(List<TexIndexAndWeightModel> dataList)
	{
		float totalWeight = 0;
		//还是去掉最后一个数据，避免计算舍弃的Weight
		for (int i = 0; i < dataList.Count - 1; i++)
		{
			totalWeight += dataList[i].TexWeight;
		}
		for (int i = 0; i < dataList.Count - 1; i++)
		{
			dataList[i].SetWeight(dataList[i].TexWeight / totalWeight);
		}
	}

	public static void TestNoSort(List<TexIndexAndWeightModel> dataList)
	{
		if (dataList[3].TexWeight >dataList[2].TexWeight)
		{
			dataList[2].SetValue(dataList[3].TexIndex, dataList[3].TexWeight);
		}
		if (dataList[3].TexWeight >dataList[1].TexWeight)
		{
			dataList[1].SetValue(dataList[3].TexIndex, dataList[3].TexWeight);
		}
		if (dataList[3].TexWeight >dataList[0].TexWeight)
		{
			dataList[0].SetValue(dataList[3].TexIndex, dataList[3].TexWeight);
		}
	}
}

/// <summary>
/// 其实和TexIndexAndWeightModel没多大关系，但是需要的功能很类似，就继承一下
/// 只比父类多一个mModelIndex，用于存储，计算
/// </summary>
public class IndexSortingModel : TexIndexAndWeightModel
{
	private int mModelIndex = 0;
	public int ModelIndex
	{
		get { return mModelIndex; }
	}

	#region Constructor

	private IndexSortingModel(int maxTexNum) : base(maxTexNum)
	{
	}

	private IndexSortingModel(int maxTexNum, int index, float weight) : base(maxTexNum, index, weight)
	{
	}

	private IndexSortingModel(int maxTexNum, float index, float weight) : base(maxTexNum, index, weight)
	{
	}

	public IndexSortingModel(int modelIndex, int maxTexNum):base(maxTexNum)
	{
		mModelIndex = modelIndex;
	}
	
	public IndexSortingModel(int modelIndex, int maxTexNum, int index, float weight) : base(maxTexNum, index, weight)
	{
		mModelIndex = modelIndex;
	}

	public IndexSortingModel(int modelIndex, int maxTexNum, float index, float weight) : base(maxTexNum, index, weight)
	{
		mModelIndex = modelIndex;
	}

	#endregion

	public void SetValueWithModelIndex(int modelIndex, int texIndex, float weight)
	{
		mModelIndex = modelIndex;
		SetValue(texIndex, weight);
	}

	public override void Reset()
	{
		mModelIndex = -1;
		base.Reset();
	}
}

public class TexAreaCalWithBounder
{
	//TODO:这个，之后再考虑动态修改
	//从0开始，所以end是512 - 1
	private int mCurTexSize = 511;
	
	//左，下，右，上，是否是边界
	private bool[] mIsBounder = new bool[4];
	public bool[] IsBounder => mIsBounder;


	//初始起止位置
	private int mStartX;
	private int mStartY;
	private int mEndX;
	private int mEndY;

	//需要计算的起止位置(如果不是边界则往外围+1)
	public int CalStartX {
		get
		{
			if (mIsBounder[0])
			{
				return mStartX;
			}

			return mStartX - 1 < 0 ? mStartX : mStartX - 1;
		}
	}
	public int CalStartY {
		get
		{
			if (mIsBounder[1])
			{
				return mStartY;
			}

			return mStartY - 1 < 0 ? mStartY : mStartY - 1;
		}
	}
	public int CalEndX {
		get
		{
			if (mIsBounder[2])
			{
				return mEndX;
			}

			return mEndX + 1 > mCurTexSize ? mEndX : mEndX + 1;
		}
	}
	public int CalEndY {
		get
		{
			if (mIsBounder[3])
			{
				return mEndY;
			}

			return mEndY + 1 > mCurTexSize ? mEndY : mEndY + 1;
		}
	}

	//初始宽高
	public int OriWidth
	{
		get { return mEndX - mStartX + 1; }
	}
	public int OriHeight
	{
		get { return mEndY - mStartY + 1; }
	}

	//CalStart/End X/Y会把数值处理好，所以这里不做多余判断，不然没完没了了
	public int CalWidth
	{
		get
		{
			return CalEndX - CalStartX + 1;
		}
	}
	public int CalHeight
	{
		get
		{
			return CalEndY - CalStartY + 1;
		}
	}

	//偏移数值，直接+就好，正负已处理
	//不过End好像也用不到
	public int OffStartX
	{
		get { return mIsBounder[0] ? 0 : 1; }
	}
	public int OffStartY
	{
		get { return mIsBounder[1] ? 0 : 1; }
	}
	public int OffEndX
	{
		get { return mIsBounder[2] ? 0 : -1; }
	}
	public int OffEndY
	{
		get { return mIsBounder[3] ? 0 : -1; }
	}

	private TexAreaCalWithBounder()
	{
	}

	public TexAreaCalWithBounder(int x, int y, int width, int hegith, int maxSize)
	{
		mCurTexSize = maxSize;
		mStartX = x;
		mStartY = y;
		mEndX = x + width - 1;
		mEndY = y + hegith - 1;
		
		mIsBounder[0] = mStartX == 0;
		mIsBounder[1] = mStartY == 0;
		mIsBounder[2] = mEndX == mCurTexSize - 1;
		mIsBounder[3] = mEndY == mCurTexSize - 1;
	}

	public void Refresh(int x, int y, int width, int hegith, int maxSize)
	{
		mCurTexSize = maxSize;
		mStartX = x;
		mStartY = y;
		//。。。针的要细
		mEndX = x + width - 1;
		mEndY = y + hegith - 1;
		
		mIsBounder[0] = mStartX == 0;
		mIsBounder[1] = mStartY == 0;
		mIsBounder[2] = mEndX == mCurTexSize;
		mIsBounder[3] = mEndY == mCurTexSize;

		if (false)
		{
			Debug.LogError(this.ToString());
		}
	}

	public override string ToString()
	{
		return $"mStartX:{mStartX}, mStartY:{mStartY}, mEndX:{mEndX}, mEndY:{mEndY}\n" +
		       $"CalStartX:{CalStartX}, CalStartY:{CalStartY}, CalEndX:{CalEndX}, CalEndY:{CalEndY}";
	}
}