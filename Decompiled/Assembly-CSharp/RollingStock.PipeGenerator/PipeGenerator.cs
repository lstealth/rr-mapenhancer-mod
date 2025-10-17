using System.Collections.Generic;
using UnityEngine;

namespace RollingStock.PipeGenerator;

public class PipeGenerator : MonoBehaviour
{
	public Vector3 defaultRotation;

	[TextArea(5, 10)]
	public string script;

	private List<Pipe> _pipes;
}
