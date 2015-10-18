namespace BlackFox.U2F.Codec
{
    public enum ApduResponseStatus : ushort
    {
        /// <summary>
        /// No Error
        /// </summary>
        NoError = 0x9000,

        /// <summary>
        /// Response bytes remaining
        /// </summary>
        BytesRemaining = 0x6100,

        /// <summary>
        /// Wrong length
        /// </summary>
        WrongLength = 0x6700,

        /// <summary>
        /// Security condition not satisfied
        /// </summary>
        SecurityStatusNotSatisfied = 0x6982,

        /// <summary>
        /// File invalid
        /// </summary>
        FileInvalid = 0x6983,

        /// <summary>
        /// Data invalid
        /// </summary>
        DataInvalid = 0x6984,

        /// <summary>
        /// Conditions of use not satisfied
        /// </summary>
        ConditionsNotSatisfied = 0x6985,

        /// <summary>
        /// Command not allowed (no current EF)
        /// </summary>
        CommandNotAllowed = 0x6986,

        /// <summary>
        /// Applet selection failed
        /// </summary>
        AppletSelectFailed = 0x6999,

        /// <summary>
        /// Wrong data
        /// </summary>
        WrongData = 0x6A80,

        /// <summary>
        /// Function not supported
        /// </summary>
        FuncNotSupported = 0x6A81,

        /// <summary>
        /// File not found
        /// </summary>
        FileNotFound = 0x6A82,

        /// <summary>
        /// Record not found
        /// </summary>
        RecordNotFound = 0x6A83,

        /// <summary>
        /// Incorrect parameters (P1,P2)
        /// </summary>
        IncorrectP1P2 = 0x6A86,

        /// <summary>
        /// Incorrect parameters (P1,P2)
        /// </summary>
        WrongP1P2 = 0x6B00,

        /// <summary>
        /// Correct Expected Length (Le)
        /// </summary>
        CorrectLength00 = 0x6C00,

        /// <summary>
        /// 'INS' value not supported
        /// </summary>
        InsNotSupported = 0x6D00,

        /// <summary>
        /// 'CLA' value not supported
        /// </summary>
        ClaNotSupported = 0x6E00,

        /// <summary>
        /// No precise diagnosis
        /// </summary>
        Unknown = 0x6F00,

        /// <summary>
        /// Not enough memory space in the file
        /// </summary>
        FileFull = 0x6A84,

        /// <summary>
        /// Card does not support the operation on the specified logical channel
        /// </summary>
        LogicalChannelNotSupported = 0x6881,

        /// <summary>
        /// Card does not support secure messaging
        /// </summary>
        SecureMessagingNotSupported = 0x6882,

        /// <summary>
        /// Warning, card state unchanged
        /// </summary>
        WarningStateUnchanged = 0x6200,

        /// <summary>
        /// Last command in chain expected
        /// </summary>
        LastCommandExpected = 0x6883,

        /// <summary>
        /// Command chaining not supported
        /// </summary>
        CommandChainingNotSupported = 0x6884
    }
}