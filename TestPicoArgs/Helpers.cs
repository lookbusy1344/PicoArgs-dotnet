using PicoArgs_dotnet;

namespace TestPicoArgs;

internal static partial class Helpers
{
	/// <summary>
	/// Try the action, and assert that it throws the expected exception
	/// </summary>
	public static void AssertThrows<E>(Action action, string errmsg) where E : Exception
	{
		var result = CheckThrows<E>(action);
		if (!result)
			Assert.Fail(errmsg);
		else
			Assert.True(true);
	}

	/// <summary>
	/// Try the action, and assert that it throws the expected PicArgsException and code
	/// </summary>
	public static void AssertPicoThrows(Action action, string errmsg, int? code)
	{
		var result = false; // assume test fails

		try
		{
			action();
		}
		catch (PicoArgsException ex)
		{
			if (code == null)
				result = true;  // true if no code specified
			else
				result = ex.Code == code; // true if code matches
		}
		catch
		{
			// some other exception was thrown, test failed
			result = false;
		}

		if (!result)
			Assert.Fail(errmsg);
		else
			Assert.True(true);
	}

	/// <summary>
	/// Try the action, and if it throws the expected exception, return true
	/// </summary>
	public static bool CheckThrows<E>(Action action) where E : Exception
	{
		try
		{
			action();
		}
		catch (E)
		{
			// expected exception was thrown, test passed
			return true;
		}
		catch
		{
			// some other exception was thrown, test failed
			return false;
		}

		// no exception was thrown, test failed
		return false;
	}

	/// <summary>
	/// Compare the lengths, and sort and compare the arrays
	/// </summary>
	public static bool CompareNames(string[] a, string[] b)
	{
		if (a.Length != b.Length) return false;

		return a.OrderBy(s => s)
			.SequenceEqual(b.OrderBy(s => s));
	}
}
