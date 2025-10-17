using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Core;
using Serilog;
using UnityEngine;

namespace RollingStock.PipeGenerator;

public class PipeLogoParser
{
	private readonly List<Pipe> _pipes = new List<Pipe>();

	private Vector3 _position = Vector3.zero;

	private readonly Quaternion _defaultRotation;

	private Quaternion _rotation;

	private PipeLogoParser(Quaternion defaultRotation)
	{
		_defaultRotation = defaultRotation;
		_rotation = _defaultRotation;
	}

	public static List<Pipe> Parse(string script, Quaternion defaultRotation)
	{
		if (script == null)
		{
			return new List<Pipe>();
		}
		PipeLogoParser pipeLogoParser = new PipeLogoParser(defaultRotation);
		string[] array = script.Split("\n");
		for (int i = 0; i < array.Length; i++)
		{
			string line = array[i];
			try
			{
				pipeLogoParser.ParseLine(line);
			}
			catch (Exception exception)
			{
				Log.Error(exception, $"Exception on line {i + 1}");
			}
		}
		return pipeLogoParser._pipes;
	}

	private void ParseLine(string line)
	{
		line = Regex.Replace(line, "(\\s*(\\/\\/|#) .*)$", "");
		string[] array = Regex.Split(line, "\\s");
		if (array.Length == 0)
		{
			return;
		}
		string text = array[0];
		string[] subArray = array[1..];
		switch (text)
		{
		default:
			if (text.Length != 0)
			{
				goto case null;
			}
			break;
		case "start":
			HandleMove(subArray);
			break;
		case "rotate":
			HandleRotate(subArray);
			break;
		case "straight":
			HandleStraight(subArray);
			break;
		case "elbow":
			HandleElbow(subArray);
			break;
		case null:
			Log.Warning("Unknown command: {command}", text);
			break;
		}
	}

	private void HandleMove(string[] arguments)
	{
		AssertArgumentsLength(arguments, 3);
		_position = Vector3FromArguments(arguments);
		_rotation = _defaultRotation;
	}

	private void HandleRotate(string[] arguments)
	{
		AssertArgumentsLength(arguments, 1);
		float z = float.Parse(arguments[0]);
		_rotation = Quaternion.Euler(0f, 0f, z) * _rotation;
	}

	private void HandleStraight(string[] arguments)
	{
		AssertArgumentsLength(arguments, 2);
		float num = float.Parse(arguments[0]);
		float num2 = float.Parse(arguments[1]);
		Vector3 position = _position;
		Vector3 vector = _position + _rotation * (Vector3.forward * num2);
		Vector3 vector2 = Vector3.Lerp(position, vector, 0.5f);
		Vector3 vector3 = _rotation * Vector3.up;
		_pipes.Add(new Pipe(new BezierCurve(position, vector2, vector2, vector, vector3, vector3), num * 0.5f));
		_position = vector;
	}

	private void HandleElbow(string[] arguments)
	{
		Quaternion rotation = _rotation;
		AssertArgumentsLength(arguments, 3);
		Vector3 vector = Vector3FromArguments(arguments);
		float x = vector.x;
		float y = vector.y;
		float z = vector.z;
		float num = MathF.PI * 2f * y * Mathf.Abs(z) / 360f * 0.41f;
		Vector3 position = _position;
		Vector3 p = _position + _rotation * (num * Vector3.forward);
		Vector3 up = _rotation * Vector3.up;
		Vector3 vector2 = _rotation * (Mathf.Sign(z) * y * Vector3.right);
		Vector3 vector3 = _position + vector2;
		Quaternion quaternion = _rotation * Quaternion.Euler(0f, z, 0f);
		_position = vector3 + quaternion * -vector2;
		_rotation = quaternion * _rotation;
		Vector3 position2 = _position;
		Vector3 p2 = _position + _rotation * (num * Vector3.back);
		Vector3 up2 = _rotation * Vector3.up;
		Pipe item = new Pipe(new BezierCurve(position, p, p2, position2, up, up2), x * 0.5f);
		item.RotationA = rotation;
		item.RotationB = _rotation;
		_pipes.Add(item);
	}

	private Vector3 Vector3FromArguments(string[] arguments, int offset = 0)
	{
		float x = float.Parse(arguments[offset]);
		float y = float.Parse(arguments[offset + 1]);
		float z = float.Parse(arguments[offset + 2]);
		return new Vector3(x, y, z);
	}

	private void AssertArgumentsLength(string[] arguments, int length)
	{
		if (arguments.Length != length)
		{
			throw new ArgumentException($"Expected {length} arguments, got {arguments.Length}");
		}
	}
}
