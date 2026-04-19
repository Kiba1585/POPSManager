using System;
using System.Collections.Generic;
using System.Text;

namespace POPSManager.Core.Integrity
{
    public enum IntegritySeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Un problema detectado durante la validación de integridad.
    /// </summary>
    public sealed class IntegrityIssue
    {
        public IntegritySeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }

        public IntegrityIssue(IntegritySeverity severity, string code, string message)
        {
            Severity = severity;
            Code = code;
            Message = message;
        }

        public override string ToString() => $"[{Severity}] {Code}: {Message}";
    }

    /// <summary>
    /// Reporte de integridad que contiene una lista de problemas.
    /// </summary>
    public sealed class IntegrityReport
    {
        private readonly List<IntegrityIssue> _issues = new();

        public IReadOnlyList<IntegrityIssue> Issues => _issues;
        public bool HasErrors => _issues.Exists(i => i.Severity == IntegritySeverity.Error);
        public bool HasWarnings => _issues.Exists(i => i.Severity == IntegritySeverity.Warning);

        public void AddInfo(string code, string message) =>
            _issues.Add(new IntegrityIssue(IntegritySeverity.Info, code, message));

        public void AddWarning(string code, string message) =>
            _issues.Add(new IntegrityIssue(IntegritySeverity.Warning, code, message));

        public void AddError(string code, string message) =>
            _issues.Add(new IntegrityIssue(IntegritySeverity.Error, code, message));

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var issue in _issues)
                sb.AppendLine(issue.ToString());
            return sb.ToString();
        }
    }
}