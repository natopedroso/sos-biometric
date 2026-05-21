using System;

namespace UI_Support {
  internal class CliResult {
    public bool Ok;
    public string Status;
    public string Message;

    public int ExitCode {
      get { return Ok ? 0 : 1; }
    }

    public string ToJson() {
      return "{"
        + "\"ok\":" + (Ok ? "true" : "false") + ","
        + "\"status\":\"" + Escape(Status) + "\"," 
        + "\"message\":\"" + Escape(Message) + "\""
        + "}";
    }

    private static string Escape(string value) {
      if (value == null) return "";
      return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
  }
}
