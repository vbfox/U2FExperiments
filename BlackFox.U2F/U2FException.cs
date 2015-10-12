using System;

namespace BlackFox.U2F
{
	public class U2FException : Exception
	{
	    public U2FException()
	    {
	    }

	    public U2FException(string message) : base(message)
	    {
	    }

	    public U2FException(string message, Exception innerException) : base(message, innerException)
	    {
	    }
	}
}
