namespace Game.AccessControl;

public struct AuthorizationRequirementInfo
{
	public readonly AuthorizationRequirement Requirement;

	public readonly object Object;

	public AuthorizationRequirementInfo(AuthorizationRequirement requirement, object o = null)
	{
		Requirement = requirement;
		Object = o;
	}

	public static implicit operator AuthorizationRequirementInfo(AuthorizationRequirement s)
	{
		return new AuthorizationRequirementInfo(s);
	}
}
