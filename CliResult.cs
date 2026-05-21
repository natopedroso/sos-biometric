using System;

namespace UI_Support {
  internal class CliResult {
    public bool Ok;
    public string Status;
    public string Message;
    public int? UserId;

    public int ExitCode {
      get { return Ok ? 0 : 1; }
    }

    public string ToJson() {
      string userIdJson = UserId.HasValue ? UserId.Value.ToString() : "null";
      return "{"
        + "\"ok\":" + (Ok ? "true" : "false") + ","
        + "\"status\":\"" + Escape(Status) + "\"," 
        + "\"message\":\"" + Escape(Message) + "\","
        + "\"userId\":" + userIdJson
        + "}";
    }

    private static string Escape(string value) {
      if (value == null) return "";
      return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
  }
}
