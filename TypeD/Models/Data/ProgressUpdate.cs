namespace TypeD.Models.Data
{
    /// <summary>Describes the progress of a long running operation, with an optional running subtask.</summary>
    /// <param name="Progress">Overall progress, 0-100.</param>
    /// <param name="Message">What the operation is currently doing.</param>
    /// <param name="SubTask">What the current subtask is doing, or <c>null</c> if there is none.</param>
    /// <param name="SubProgress">Progress of the subtask, 0-100, or <c>null</c> if it can't be measured.</param>
    public record ProgressUpdate(int Progress, string Message, string SubTask = null, int? SubProgress = null);
}
