namespace TypeD.Models.Data
{
    public class BuildIssue
    {
        public string Severity { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public string File { get; set; }
        public int? Line { get; set; }
        public int? Column { get; set; }

        public override string ToString()
        {
            var location = string.IsNullOrEmpty(File)
                ? string.Empty
                : $"{File}{(Line.HasValue ? $"({Line}{(Column.HasValue ? $",{Column}" : string.Empty)})" : string.Empty)}: ";
            var code = string.IsNullOrEmpty(Code) ? string.Empty : $"{Code}: ";
            return $"{location}{Severity} {code}{Message}";
        }
    }
}
