using System;

[AttributeUsage(AttributeTargets.Assembly)]
public class CustomAssemblyVariable : Attribute
{        
    private string _name, _value;
    public string name
	{
		get {
			return _name;
		}
	}

    public string value
	{
		get {
			return _value;
		}
	}

    public CustomAssemblyVariable(string name, string value)
    {
        this._name = name;
        this._value = value;
    }
}