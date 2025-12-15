namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrInterface
{
	public abstract string Name { get; }

	public abstract ClrInterface BaseInterface { get; }

	public override string ToString()
	{
		return Name;
	}

	public override bool Equals(object obj)
	{
		if (obj == null || !(obj is ClrInterface))
		{
			return false;
		}
		ClrInterface clrInterface = (ClrInterface)obj;
		if (Name != clrInterface.Name)
		{
			return false;
		}
		if (BaseInterface == null)
		{
			return clrInterface.BaseInterface == null;
		}
		return BaseInterface.Equals(clrInterface.BaseInterface);
	}

	public override int GetHashCode()
	{
		int num = 0;
		if (Name != null)
		{
			num ^= Name.GetHashCode();
		}
		if (BaseInterface != null)
		{
			num ^= BaseInterface.GetHashCode();
		}
		return num;
	}
}
