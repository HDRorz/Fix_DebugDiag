namespace DebugDiag.DbgEng;

public enum DEBUG_REQUEST : uint
{
	/// <summary>
	/// InBuffer - Unused.
	/// OutBuffer - Unused.
	/// </summary>
	SOURCE_PATH_HAS_SOURCE_SERVER,
	/// <summary>
	/// InBuffer - Unused.
	/// OutBuffer - Machine-specific CONTEXT.
	/// </summary>
	TARGET_EXCEPTION_CONTEXT,
	/// <summary>
	/// InBuffer - Unused.
	/// OutBuffer - ULONG system ID of thread.
	/// </summary>
	TARGET_EXCEPTION_THREAD,
	/// <summary>
	/// InBuffer - Unused.
	/// OutBuffer - EXCEPTION_RECORD64.
	/// </summary>
	TARGET_EXCEPTION_RECORD,
	/// <summary>
	/// InBuffer - Unused.
	/// OutBuffer - DEBUG_CREATE_PROCESS_OPTIONS.
	/// </summary>
	GET_ADDITIONAL_CREATE_OPTIONS,
	/// <summary>
	/// InBuffer - DEBUG_CREATE_PROCESS_OPTIONS.
	/// OutBuffer - Unused.
	/// </summary>
	SET_ADDITIONAL_CREATE_OPTIONS,
	/// <summary>
	/// InBuffer - Unused.
	/// OutBuffer - ULONG[2] major/minor.
	/// </summary>
	GET_WIN32_MAJOR_MINOR_VERSIONS,
	/// <summary>
	/// InBuffer - DEBUG_READ_USER_MINIDUMP_STREAM.
	/// OutBuffer - Unused.
	/// </summary>
	READ_USER_MINIDUMP_STREAM,
	/// <summary>
	/// InBuffer - Unused.
	/// OutBuffer - Unused.
	/// </summary>
	TARGET_CAN_DETACH,
	/// <summary>
	/// InBuffer - PTSTR.
	/// OutBuffer - Unused.
	/// </summary>
	SET_LOCAL_IMPLICIT_COMMAND_LINE,
	/// <summary>
	/// InBuffer - Unused.
	/// OutBuffer - Event code stream offset.
	/// </summary>
	GET_CAPTURED_EVENT_CODE_OFFSET,
	/// <summary>
	/// InBuffer - Unused.
	/// OutBuffer - Event code stream information.
	/// </summary>
	READ_CAPTURED_EVENT_CODE_STREAM,
	/// <summary>
	/// InBuffer - Input data block.
	/// OutBuffer - Processed data block.
	/// </summary>
	EXT_TYPED_DATA_ANSI,
	/// <summary>
	/// InBuffer - Unused.
	/// OutBuffer - Returned path.
	/// </summary>
	GET_EXTENSION_SEARCH_PATH_WIDE,
	/// <summary>
	/// InBuffer - DEBUG_GET_TEXT_COMPLETIONS_IN.
	/// OutBuffer - DEBUG_GET_TEXT_COMPLETIONS_OUT.
	/// </summary>
	GET_TEXT_COMPLETIONS_WIDE,
	/// <summary>
	/// InBuffer - ULONG64 cookie.
	/// OutBuffer - DEBUG_CACHED_SYMBOL_INFO.
	/// </summary>
	GET_CACHED_SYMBOL_INFO,
	/// <summary>
	/// InBuffer - DEBUG_CACHED_SYMBOL_INFO.
	/// OutBuffer - ULONG64 cookie.
	/// </summary>
	ADD_CACHED_SYMBOL_INFO,
	/// <summary>
	/// InBuffer - ULONG64 cookie.
	/// OutBuffer - Unused.
	/// </summary>
	REMOVE_CACHED_SYMBOL_INFO,
	/// <summary>
	/// InBuffer - DEBUG_GET_TEXT_COMPLETIONS_IN.
	/// OutBuffer - DEBUG_GET_TEXT_COMPLETIONS_OUT.
	/// </summary>
	GET_TEXT_COMPLETIONS_ANSI,
	/// <summary>
	/// InBuffer - Unused.
	/// OutBuffer - Unused.
	/// </summary>
	CURRENT_OUTPUT_CALLBACKS_ARE_DML_AWARE,
	/// <summary>
	/// InBuffer - ULONG64 offset.
	/// OutBuffer - Unwind information.
	/// </summary>
	GET_OFFSET_UNWIND_INFORMATION,
	/// <summary>
	/// InBuffer - Unused
	/// OutBuffer - returned DUMP_HEADER32/DUMP_HEADER64 structure.
	/// </summary>
	GET_DUMP_HEADER,
	/// <summary>
	/// InBuffer - DUMP_HEADER32/DUMP_HEADER64 structure.
	/// OutBuffer - Unused
	/// </summary>
	SET_DUMP_HEADER,
	/// <summary>
	/// InBuffer - Midori specific
	/// OutBuffer - Midori specific
	/// </summary>
	MIDORI,
	/// <summary>
	/// InBuffer - Unused
	/// OutBuffer - PROCESS_NAME_ENTRY blocks
	/// </summary>
	PROCESS_DESCRIPTORS,
	/// <summary>
	/// InBuffer - Unused
	/// OutBuffer - MINIDUMP_MISC_INFO_N blocks
	/// </summary>
	MISC_INFORMATION,
	/// <summary>
	/// InBuffer - Unused
	/// OutBuffer - ULONG64 as TokenHandle value
	/// </summary>
	OPEN_PROCESS_TOKEN,
	/// <summary>
	/// InBuffer - Unused
	/// OutBuffer - ULONG64 as TokenHandle value
	/// </summary>
	OPEN_THREAD_TOKEN,
	/// <summary>
	/// InBuffer -  ULONG64 as TokenHandle being duplicated
	/// OutBuffer - ULONG64 as new duplicated TokenHandle
	/// </summary>
	DUPLICATE_TOKEN,
	/// <summary>
	/// InBuffer - a ULONG64 as TokenHandle and a ULONG as NtQueryInformationToken() request code
	/// OutBuffer - NtQueryInformationToken() return
	/// </summary>
	QUERY_INFO_TOKEN,
	/// <summary>
	/// InBuffer - ULONG64 as TokenHandle
	/// OutBuffer - Unused
	/// </summary>
	CLOSE_TOKEN,
	/// <summary>
	/// InBuffer - ULONG64 for process server identification and ULONG as PID
	/// OutBuffer - Unused
	/// </summary>
	WOW_PROCESS,
	/// <summary>
	/// InBuffer - ULONG64 for process server identification and PWSTR as module path
	/// OutBuffer - Unused
	/// </summary>
	WOW_MODULE,
	/// <summary>
	/// InBuffer - Unused
	/// OutBuffer - Unused
	/// return - S_OK if non-invasive user-mode attach, S_FALSE if not (but still live user-mode), E_FAIL otherwise.
	/// </summary>
	LIVE_USER_NON_INVASIVE,
	/// <summary>
	/// InBuffer - TID
	/// OutBuffer - Unused
	/// return - ResumeThreads() return.
	/// </summary>
	RESUME_THREAD
}
