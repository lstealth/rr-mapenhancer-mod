using Game.Messages;
using HeathenEngineering.SteamworksIntegration.API;
using Model;
using Serilog;
using Serilog.Events;
using Track;
using UnityEngine;

namespace Logging;

public class LogManager : MonoBehaviour
{
	private void Awake()
	{
		Log.Logger = MakeConfiguration().MinimumLevel.Information().MinimumLevel.Override("Model.AI.AutoEngineer", LogEventLevel.Warning).MinimumLevel.Override("Model.AI.AutoEngineerPlanner", LogEventLevel.Warning).MinimumLevel.Override("Effects.Decals.CanvasDecalRenderer", LogEventLevel.Warning).CreateLogger();
		Log.Information("Railroader {appVersion} ({buildId})", Application.version, App.Client.BuildId);
	}

	private void OnDestroy()
	{
		Log.CloseAndFlush();
	}

	private static LoggerConfiguration MakeConfiguration()
	{
		return new LoggerConfiguration().Destructure.ByTransforming((Vector3 v) => new
		{
			X = v.x,
			Y = v.y,
			Z = v.z
		}).Destructure.ByTransforming(delegate(Quaternion q)
		{
			Vector3 eulerAngles = q.eulerAngles;
			return new
			{
				EulerX = eulerAngles.x,
				EulerY = eulerAngles.y,
				EulerZ = eulerAngles.z
			};
		}).Destructure.ByTransforming((Location l) => new
		{
			Id = l.segment.id,
			Distance = l.distance,
			EndIsA = (l.end == TrackSegment.End.A)
		}).Destructure.ByTransforming((Snapshot.TrackLocation l) => new
		{
			Id = l.segmentId,
			Distance = l.distance,
			EndIsA = l.endIsA
		}).Destructure.ByTransforming((Car c) => new
		{
			Id = c.id,
			Name = c.DisplayName
		}).WriteTo.UnityConsole("[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}");
	}
}
