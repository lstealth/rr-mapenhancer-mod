using Model.Definition.Data;
using UnityEngine;

namespace Model;

public interface IDefinitionReferenceResolver
{
	Transform Resolve(TransformReference transformReference);

	AnimationClip Resolve(AnimationReference animationReference);
}
