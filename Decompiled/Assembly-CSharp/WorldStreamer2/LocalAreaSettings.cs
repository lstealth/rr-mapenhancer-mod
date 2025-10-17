using System.Collections.Generic;
using UnityEngine;

namespace WorldStreamer2;

public class LocalAreaSettings : ScriptableObject
{
	public bool collectionsCollapsed = true;

	public int listSizeCollections;

	public List<SceneCollectionManager> currentCollections = new List<SceneCollectionManager>();

	public bool showLoadingPoint = true;

	public int distanceFromCenter;

	public bool tiles;

	public Vector3 CenterPoint;
}
