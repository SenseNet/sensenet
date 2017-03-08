using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.ContentRepository.Versioning
{
	public enum VersioningType
	{
		Inherited = 0,
		None = 1,
		MajorOnly = 2,
		MajorAndMinor = 3
	}

	public enum InheritableVersioningType
	{
		Inherited = 0,
		None = 1,
		MajorOnly = 2,
		MajorAndMinor = 3
	}

	public enum ApprovingType
	{
		Inherited = 0,
		False = 1,
		True = 2
	}

    public enum StateAction
    {
        Save = 1,
        CheckOut = 2,
        CheckIn = 3,
        UndoCheckOut = 4,
        Publish = 5,
        Approve = 6,
        Reject = 7,
        SaveAndCheckIn = 8
    }

}