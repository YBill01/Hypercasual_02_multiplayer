using GameName.Data;
using UnityEngine;

public interface ICollectingSource<T> where T : struct
{
	CollectingData CollectingData();

	void CollectingStart(T collectingInfo);
	void CollectingEnd(T collectingInfo);
}