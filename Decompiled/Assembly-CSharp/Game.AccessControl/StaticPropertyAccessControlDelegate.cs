namespace Game.AccessControl;

public readonly struct StaticPropertyAccessControlDelegate : IPropertyAccessControlDelegate
{
	private readonly AuthorizationRequirementInfo _req;

	public StaticPropertyAccessControlDelegate(AuthorizationRequirementInfo req)
	{
		_req = req;
	}

	public AuthorizationRequirementInfo AuthorizationRequirementForPropertyWrite(string key)
	{
		return _req;
	}
}
