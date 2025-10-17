using System.Collections.Generic;
using System.Linq;
using Core;
using CorgiSpline;
using Helpers;
using Map.Runtime;
using Map.Runtime.MapModifiers;
using Map.Runtime.MaskComponents;
using UnityEngine;

namespace Track;

public class TrackObjectBuilder
{
	private struct PointDirection
	{
		public Vector3 Position;

		public readonly Quaternion Rotation;

		public PointDirection(Vector3 position, Quaternion rotation)
		{
			Position = position;
			Rotation = rotation;
		}
	}

	public const string TagGenerated = "TrackMeshGenerated";

	private const float TieSpacing = 0.55f;

	private readonly TrackMeshProfile _profile;

	private readonly HideFlags _meshHideFlags;

	private readonly GameObject _parent;

	private readonly int _trackLayer;

	private readonly PrefabInstancer _prefabInstancer;

	private bool _warnedMissingTieInstancer;

	public TrackObjectBuilder(TrackMeshProfile profile, GameObject parent, PrefabInstancer prefabInstancer, HideFlags meshHideFlags)
	{
		_profile = profile;
		_meshHideFlags = meshHideFlags;
		_parent = parent;
		_prefabInstancer = prefabInstancer;
		_trackLayer = Layers.Track;
	}

	private GameObject CreateGeneratedObjectContainer()
	{
		GameObject gameObject = new GameObject();
		gameObject.hideFlags = _meshHideFlags;
		gameObject.layer = _trackLayer;
		gameObject.tag = "TrackMeshGenerated";
		gameObject.name = "Generated";
		gameObject.SetActive(value: false);
		gameObject.transform.SetParent(_parent.transform, worldPositionStays: false);
		return gameObject;
	}

	public GameObject CreateSegmentObject(SegmentProxy segment)
	{
		GameObject gameObject = CreateGeneratedObjectContainer();
		CreateSegmentObject(segment, gameObject.transform);
		CreateRoadbed(segment.Curve, gameObject.transform, segment.Segment.style);
		gameObject.SetActive(value: true);
		return gameObject;
	}

	public GameObject CreateSegmentMasks(SegmentProxy segment)
	{
		GameObject gameObject = CreateGeneratedObjectContainer();
		gameObject.name = "Mask Container";
		BuildMasks(segment.Curve, gameObject, segment.Segment.style, segment.Segment.id);
		gameObject.SetActive(value: true);
		return gameObject;
	}

	private static IEnumerable<LinePoint> CurveToSplinePoints(BezierCurve curve)
	{
		return curve.Approximate(1.00001f, 5f);
	}

	public GameObject CreateSwitchObject(SwitchGeometry geometry, TrackNode node, BezierCurve aCurve, BezierCurve aRoadbedCurve, BezierCurve bCurve, BezierCurve bRoadbedCurve)
	{
		GameObject gameObject = CreateGeneratedObjectContainer();
		gameObject.transform.localPosition = geometry.switchHome;
		Transform transform = gameObject.transform;
		CreateSwitchObjectHelper(geometry, node, transform);
		CreateMeshColliderObject(TrackMeshBuilder.BuildColliderMesh(aCurve.OffsetBy(-geometry.switchHome), Gauge.Standard), "Collider-a", transform);
		CreateMeshColliderObject(TrackMeshBuilder.BuildColliderMesh(bCurve.OffsetBy(-geometry.switchHome), Gauge.Standard), "Collider-b", transform);
		CreateRoadbed(aRoadbedCurve, transform);
		CreateRoadbed(bRoadbedCurve, transform);
		gameObject.SetActive(value: true);
		return gameObject;
	}

	public GameObject CreateSwitchMasks(SwitchGeometry geometry, TrackNode node, BezierCurve aCurve, BezierCurve aRoadbedCurve, BezierCurve bCurve, BezierCurve bRoadbedCurve, TrackSegment.Style trackSegmentStyle)
	{
		GameObject gameObject = CreateGeneratedObjectContainer();
		gameObject.name = "Mask Container";
		int num = 0;
		BezierCurve[] array = new BezierCurve[2] { aRoadbedCurve, bRoadbedCurve };
		foreach (BezierCurve bezierCurve in array)
		{
			BezierCurve curve = bezierCurve.OffsetBy(geometry.switchHome);
			BuildMasks(curve, gameObject, trackSegmentStyle, node.id + "-" + num);
			num++;
		}
		StaticMapMask staticMapMask = gameObject.AddComponent<StaticMapMask>();
		staticMapMask.CoordinateSystem = MapManager.CoordinateSystem.Game;
		Vector3 center = geometry.switchHome + geometry.standPosition + geometry.standRotation * (2f * Vector3.left) - 0.05f * Vector3.up;
		staticMapMask.AddModifier(new HeightmapModifier(1, HeightmapModifierKind.BlendHeight, new RectangleMaskDescriptor(center, 0.1f * Vector3.one, 0f, 0.75f, 2f)));
		gameObject.SetActive(value: true);
		return gameObject;
	}

	private void BuildMasks(BezierCurve curve, GameObject parent, TrackSegment.Style style, string key)
	{
		RoadbedBuilder.BuildMasks(curve, parent, style, key);
	}

	public GameObject CreateBumperObject(TrackNode node, Vector3 direction, TrackSegment.Style style)
	{
		GameObject gameObject = CreateGeneratedObjectContainer();
		Transform transform = gameObject.transform;
		CreateBumperModel(node, direction, transform);
		Quaternion quaternion = Quaternion.LookRotation(direction);
		Vector3 localPosition = node.transform.localPosition;
		Vector3 forward = Vector3.forward;
		float num = 1.25f;
		Vector3 up = node.transform.up;
		BezierCurve curve = new BezierCurve(localPosition, localPosition + quaternion * forward * 0.1f, localPosition + quaternion * forward * (num - 0.1f), localPosition + quaternion * forward * num, up, up);
		CreateTrackObject(curve, 0.55f, 0.08f, "bumper-track", transform);
		gameObject.SetActive(value: true);
		return gameObject;
	}

	public GameObject CreateBumperMasks(TrackNode node, Vector3 direction, TrackSegment.Style style)
	{
		GameObject gameObject = CreateGeneratedObjectContainer();
		gameObject.name = "Mask Container";
		Vector3 localPosition = node.transform.localPosition;
		Vector3 up = node.transform.up;
		Vector3 p = localPosition;
		Vector3 p2 = localPosition + direction * 2f;
		Vector3 vector = localPosition + direction * 1f;
		RoadbedBuilder.BuildMasks(new BezierCurve(p, vector, vector, p2, up, up), gameObject, style, node.id);
		gameObject.SetActive(value: true);
		return gameObject;
	}

	private void CreateSegmentObject(SegmentProxy segment, Transform parent)
	{
		float tieSpacing = 0.55f * ((segment.Segment.style == TrackSegment.Style.Bridge) ? 0.6f : 1f);
		float tieSpacingJitter = ((segment.Segment.style == TrackSegment.Style.Bridge) ? 0.02f : 0.08f);
		CreateTrackObject(segment.Curve, tieSpacing, tieSpacingJitter, "seg-" + segment.Segment.id, parent);
	}

	private void CreateTrackObject(BezierCurve curve, float tieSpacing, float tieSpacingJitter, string trackName, Transform parent)
	{
		Vector3 endPoint = curve.EndPoint1;
		curve = curve.OffsetBy(-endPoint);
		GameObject gameObject = new GameObject
		{
			hideFlags = _meshHideFlags,
			layer = _trackLayer,
			name = trackName,
			tag = "TrackMeshGenerated"
		};
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		gameObject.transform.localPosition = endPoint;
		Gauge standard = Gauge.Standard;
		SwitchGeometry.RailLineCurves railLineCurves = SwitchGeometry.MakeTrackLineSegments(curve, standard);
		CreateMeshObject(TrackMeshBuilder.BuildStockRailMesh(railLineCurves.left, endPoint, standard, (int _) => 1f), "L", gameObject);
		CreateMeshObject(TrackMeshBuilder.BuildStockRailMesh(railLineCurves.right, endPoint, standard, (int _) => 1f), "R", gameObject);
		CreateTies(curve, tieSpacing, tieSpacingJitter, gameObject.transform);
		CreateMeshColliderObject(TrackMeshBuilder.BuildColliderMesh(curve, standard), "Collider", gameObject.transform);
	}

	private void CreateRoadbed(BezierCurve curve, Transform parent, TrackSegment.Style style = TrackSegment.Style.Standard)
	{
		if (style == TrackSegment.Style.Tunnel)
		{
			CreateTunnel(curve, parent);
		}
	}

	private void CreateTunnel(BezierCurve curve, Transform parent)
	{
		Vector3 endPoint = curve.EndPoint1;
		Quaternion rotation = curve.GetRotation(0f);
		Vector3 endPoint2 = curve.EndPoint2;
		Quaternion rotation2 = curve.GetRotation(1f);
		if (_profile.tunnelPortalPrefab != null)
		{
			GameObject gameObject = Object.Instantiate(_profile.tunnelPortalPrefab, parent);
			GameObject gameObject2 = Object.Instantiate(_profile.tunnelPortalPrefab, parent);
			gameObject.transform.localPosition = endPoint;
			gameObject.transform.localRotation = rotation;
			gameObject2.transform.localPosition = endPoint2;
			gameObject2.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * rotation2;
		}
		if (_profile.tunnelLinerPrefab != null)
		{
			SplineMeshBuilder_RepeatingMesh splineMeshBuilder_RepeatingMesh = Object.Instantiate(_profile.tunnelLinerPrefab, parent);
			splineMeshBuilder_RepeatingMesh.transform.localPosition = endPoint;
			Spline splineReference = splineMeshBuilder_RepeatingMesh.SplineReference;
			Vector3 offset = Vector3.up * 2f - endPoint;
			Vector3 scale = new Vector3(1.2f, 1.4f, 1f);
			splineReference.Points = (from point in Enumerable.Range(0, 4).Select(((BezierCurve)curve).GetPoint)
				select new SplinePoint(point + offset, Quaternion.identity, scale, Color.white)).ToArray();
			splineReference.Points[0].rotation = rotation;
			splineReference.Points[1].rotation = rotation;
			splineReference.Points[2].rotation = rotation2;
			splineReference.Points[3].rotation = rotation2;
			splineReference.SetSplineSpace(Space.Self, updatePoints: false);
			splineReference.UpdateNative();
		}
	}

	private void CreateSwitchObjectHelper(SwitchGeometry geometry, TrackNode node, Transform parent)
	{
		GameObject root = new GameObject
		{
			hideFlags = _meshHideFlags,
			layer = _trackLayer,
			name = "sw-" + node.id,
			tag = "TrackMeshGenerated"
		};
		root.transform.SetParent(parent, worldPositionStays: false);
		Gauge gauge = Gauge.Standard;
		CreateMeshObject(TrackMeshBuilder.BuildFrogMesh(geometry.frogPoints, gauge), "Frog", root);
		CreateMeshObject(TrackMeshBuilder.BuildStockRailMesh(geometry.leftStockRail, geometry.switchHome, gauge, (int _) => 1f), "StockL", root);
		CreateMeshObject(TrackMeshBuilder.BuildStockRailMesh(geometry.rightStockRail, geometry.switchHome, gauge, (int _) => 1f), "StockR", root);
		LineCurve aClosureRail = geometry.aClosureRail;
		LineCurve bClosureRail = geometry.bClosureRail;
		CreateMeshObject(TrackMeshBuilder.BuildStockRailMesh(aClosureRail, geometry.switchHome, gauge, (int _) => 1f), "ClosureA", root);
		CreateMeshObject(TrackMeshBuilder.BuildStockRailMesh(bClosureRail, geometry.switchHome, gauge, (int _) => 1f), "ClosureB", root);
		CreateMeshObject(TrackMeshBuilder.BuildStockRailMesh(geometry.leftGuardRail, geometry.switchHome, gauge, (int _) => 1f), "GuardA", root);
		CreateMeshObject(TrackMeshBuilder.BuildStockRailMesh(geometry.rightGuardRail, geometry.switchHome, gauge, (int _) => 1f), "GuardB", root);
		GameObject normalPointRail = CreatePointRail(geometry.aPointRail, "PointA");
		GameObject reversedPointRail = CreatePointRail(geometry.bPointRail, "PointB");
		Vector3 point = geometry.aPointRail.Points.Last().point;
		Vector3 point2 = geometry.bPointRail.Points.Last().point;
		Vector3 point3 = geometry.aPointRail.Points.First().point;
		Vector3 point4 = geometry.bPointRail.Points.First().point;
		Vector3 vector = (point3 - point4).normalized * 0.2f + point3;
		Vector3 vector2 = (point4 - point3).normalized * 0.2f + point4;
		float normalRot = Vector3.SignedAngle(vector - point, point3 - point, geometry.frogPoints[1].Rotation * Vector3.up);
		float reversedRot = Vector3.SignedAngle(vector2 - point2, point4 - point2, geometry.frogPoints[1].Rotation * Vector3.up);
		root.AddComponent<SwitchPointRails>().Configure(node, normalPointRail, reversedPointRail, normalRot, reversedRot);
		CreateSwitchStand(geometry, node, root.transform);
		CreateTies(geometry, root.transform);
		GameObject CreatePointRail(LineCurve pointRail, string objName)
		{
			Vector3 point5 = pointRail.Points.Last().point;
			Mesh mesh = TrackMeshBuilder.BuildStockRailMesh(ReprofilePointRail(pointRail).Offset(-point5), geometry.switchHome, gauge, (int i) => (i != 0) ? 1f : 0.1f);
			GameObject gameObject = CreateMeshObject(mesh, objName, root);
			gameObject.transform.localPosition = point5;
			return gameObject;
		}
	}

	private void CreateBumperModel(TrackNode node, Vector3 direction, Transform parent)
	{
		GameObject gameObject = Object.Instantiate(_profile.bumperPrefab, parent);
		gameObject.name = "bumper-" + node.id;
		gameObject.hideFlags = _meshHideFlags;
		gameObject.tag = "TrackMeshGenerated";
		gameObject.transform.localPosition = node.transform.localPosition;
		gameObject.transform.localRotation = Quaternion.LookRotation(direction);
	}

	private GameObject CreateMeshObject(Mesh mesh, string objectName, GameObject parent)
	{
		GameObject gameObject = new GameObject();
		gameObject.hideFlags = _meshHideFlags;
		gameObject.layer = _trackLayer;
		gameObject.name = objectName;
		gameObject.tag = "TrackMeshGenerated";
		gameObject.transform.SetParent(parent.transform, worldPositionStays: false);
		MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
		gameObject.AddComponent<MeshRenderer>().material = _profile.trackMaterial;
		meshFilter.mesh = mesh;
		gameObject.AddComponent<MeshDestroyer>().mesh = mesh;
		return gameObject;
	}

	private void CreateSwitchStand(SwitchGeometry geometry, TrackNode node, Transform parent)
	{
		SwitchStand switchStand = (node.IsCTCSwitch ? _profile.switchStandPrefabCTC : _profile.switchStandPrefab);
		if (switchStand == null)
		{
			Debug.LogWarning("Missing switch stand prefab");
			return;
		}
		SwitchStand switchStand2 = Object.Instantiate(switchStand, parent, worldPositionStays: true);
		switchStand2.name = "Switch Stand";
		switchStand2.hideFlags = HideFlags.DontSave;
		switchStand2.transform.localPosition = geometry.standPosition;
		switchStand2.transform.localRotation = geometry.standRotation;
		switchStand2.Configure(node);
		switchStand2.GetComponentInChildren<SwitchStandClick>().node = node;
	}

	private void CreateTies(SwitchGeometry geometry, Transform parent)
	{
		FindStraighterRail(geometry, out var straight, out var curved);
		straight = new LineCurve(straight);
		curved = new LineCurve(curved);
		Gauge standard = Gauge.Standard;
		float num = standard.Inside + 1f;
		List<Matrix4x4> ties = new List<Matrix4x4>();
		float num2 = 0f;
		straight = straight.Skip(num2);
		curved = curved.Skip(num2);
		int num3 = 0;
		while (straight.Length >= 0.55f && curved.Length >= 0.55f)
		{
			LinePoint linePoint = straight.Points.First();
			LinePoint linePoint2 = curved.Points.First();
			Vector3 center = Vector3.Lerp(linePoint.point, linePoint2.point, 0.5f);
			float zScale = ((linePoint.point - linePoint2.point).magnitude + 1f) / num;
			if (num3 != 0 && num3 != 1)
			{
				AddTie(center, linePoint.direction, zScale);
			}
			straight = straight.Skip(0.55f);
			curved = curved.Skip(0.55f);
			num3++;
		}
		float num4 = standard.Inside / 2f;
		Vector3 point = straight.Points.Last().point;
		if (straight.Length < 0.82500005f && straight.Length > 11f / 60f)
		{
			straight = straight.Skip(straight.Length / 2f);
			LinePoint linePoint3 = straight.Points.First();
			Vector3 center2 = linePoint3.point + Vector3.Cross(linePoint3.point - point, Vector3.up).normalized * num4;
			AddTie(center2, linePoint3.direction);
		}
		while (straight.Length > num2)
		{
			LinePoint linePoint4 = straight.Points.First();
			Vector3 center3 = linePoint4.point + Vector3.Cross(linePoint4.point - point, Vector3.up).normalized * num4;
			AddTie(center3, linePoint4.direction);
			straight = straight.Skip(0.55f, failSilently: true);
		}
		Vector3 point2 = curved.Points.Last().point;
		if (curved.Length < 0.82500005f && curved.Length > 11f / 60f)
		{
			curved = curved.Skip(curved.Length / 2f);
			LinePoint linePoint5 = curved.Points.First();
			Vector3 center4 = linePoint5.point + Vector3.Cross(linePoint5.point - point2, Vector3.down).normalized * num4;
			AddTie(center4, linePoint5.direction);
		}
		while (curved.Length > num2)
		{
			LinePoint linePoint6 = curved.Points.First();
			Vector3 center5 = linePoint6.point + Vector3.Cross(linePoint6.point - point2, Vector3.down).normalized * num4;
			AddTie(center5, linePoint6.direction);
			curved = curved.Skip(0.55f, failSilently: true);
		}
		CreateInstancedMeshDrawer(ties.ToArray(), geometry.switchHome, PrefabInstancer.Prefab.Tie, parent.gameObject);
		void AddTie(Vector3 point3, Vector3 dir, float zScale2 = 1f)
		{
			TieTransformValues(point3, dir, zScale2, out var position, out var rotation, out var scale);
			Matrix4x4 item = Matrix4x4.TRS(position, rotation, scale);
			ties.Add(item);
		}
	}

	private void CreateTies(BezierCurve curve, float spacing, float tieSpacingJitter, Transform parent)
	{
		Gauge standard = Gauge.Standard;
		List<PointDirection> list = new List<PointDirection>();
		List<PointDirection> list2 = new List<PointDirection>();
		float num = tieSpacingJitter / 4f;
		LineCurve lineCurve = new LineCurve(curve.Approximate(), Hand.Left);
		float num2 = Mathf.Round(lineCurve.Length / spacing);
		if (num2 != 0f)
		{
			spacing = lineCurve.Length / num2;
			float length = spacing / 2f;
			LineCurveCursor lineCurveCursor = lineCurve.CursorAtHead().Skip(length);
			for (int i = 0; (float)i < num2; i++)
			{
				LinePoint linePoint = lineCurveCursor.LinePoint();
				Vector3 point = linePoint.point;
				Quaternion rotation = linePoint.Rotation;
				Vector3 position = point + rotation * Vector3.left * Random.Range(0f - num, num);
				list.Add(new PointDirection(position, rotation));
				float num3 = standard.Inside / 2f + standard.HeadWidth / 2f;
				Vector3 position2 = point + rotation * Vector3.right * num3;
				Vector3 position3 = point + rotation * Vector3.left * num3;
				list2.Add(new PointDirection(position2, rotation));
				list2.Add(new PointDirection(position3, rotation));
				lineCurveCursor = lineCurveCursor.Skip(spacing);
			}
			Quaternion quaternion = Quaternion.Euler(90f, 90f, 0f) * Quaternion.Euler(180f, 0f, 0f);
			Matrix4x4[] array = new Matrix4x4[list.Count];
			for (int j = 0; j < list.Count; j++)
			{
				PointDirection pointDirection = list[j];
				float num4 = Mathf.PingPong(pointDirection.Position.magnitude, 0.01f);
				Vector3 vector = (0f - (Gauge.Standard.RailHeight + 0.1f) + num4) * (pointDirection.Rotation * Vector3.up);
				array[j] = Matrix4x4.TRS(pointDirection.Position + vector, pointDirection.Rotation * quaternion, Vector3.one);
			}
			CreateInstancedMeshDrawer(array, parent.localPosition, PrefabInstancer.Prefab.Tie, parent.gameObject);
			Quaternion quaternion2 = Quaternion.Euler(-90f, 0f, 0f);
			Matrix4x4[] array2 = new Matrix4x4[list2.Count];
			for (int k = 0; k < list2.Count; k++)
			{
				PointDirection pointDirection2 = list2[k];
				array2[k] = Matrix4x4.TRS(pointDirection2.Position, pointDirection2.Rotation * quaternion2, Vector3.one);
			}
			CreateInstancedMeshDrawer(array2, parent.localPosition, PrefabInstancer.Prefab.TiePlate, parent.gameObject);
		}
	}

	private void TieTransformValues(Vector3 point, Vector3 dir, float zScale, out Vector3 position, out Quaternion rotation, out Vector3 scale)
	{
		float num = Mathf.PingPong(point.magnitude, 0.01f);
		position = point + new Vector3(0f, 0f - (Gauge.Standard.RailHeight + 0.1f) + num, 0f);
		rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 90f, 0f) * Quaternion.Euler(180f, 0f, 0f);
		scale = new Vector3(1f, zScale, 1f);
	}

	private static void FindStraighterRail(SwitchGeometry geometry, out LineCurve straight, out LineCurve curved)
	{
		straight = geometry.leftStockRail;
		curved = geometry.rightStockRail;
	}

	private static LineCurve ReprofilePointRail(LineCurve curve)
	{
		LineCurve lineCurve = curve.Skip(0.2f);
		LinePoint linePoint = lineCurve.Points.First();
		LineCurve lineCurve2 = lineCurve.Skip(4f);
		lineCurve2.Insert(0, linePoint);
		return lineCurve2;
	}

	private GameObject CreateMeshColliderObject(Mesh mesh, string objectName, Transform parent)
	{
		GameObject gameObject = new GameObject();
		gameObject.hideFlags = _meshHideFlags;
		gameObject.layer = _trackLayer;
		gameObject.name = objectName;
		gameObject.tag = "TrackMeshGenerated";
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
		meshCollider.sharedMesh = mesh;
		meshCollider.convex = false;
		gameObject.AddComponent<MeshDestroyer>().mesh = mesh;
		return gameObject;
	}

	private void CreateInstancedMeshDrawer(Matrix4x4[] transforms, Vector3 offset, PrefabInstancer.Prefab prefab, GameObject parent)
	{
		if (_prefabInstancer == null)
		{
			if (!_warnedMissingTieInstancer)
			{
				Debug.LogError("Missing tieInstancer");
			}
			_warnedMissingTieInstancer = true;
			return;
		}
		Matrix4x4 matrix4x = Matrix4x4.Translate(WorldTransformer.GameToWorld(offset));
		for (int i = 0; i < transforms.Length; i++)
		{
			transforms[i] = matrix4x * transforms[i];
		}
		object obj = _prefabInstancer.AddInstances(prefab, transforms);
		if (obj != null)
		{
			parent.AddComponent<PrefabInstanceReleaseOnDestroy>().Configure(_prefabInstancer, obj);
		}
	}
}
