using System.Collections.Generic;
using UnityEngine;

namespace EverythingGrows
{
    // Shared diagnostics for the editor check and runtime preflight. Never repairs data.
    public sealed class SetupValidationReport
    {
        public sealed class Issue
        {
            public string Code, Message;
            public bool IsError;
            public Object Context;
        }
        private readonly List<Issue> issues = new();
        public IReadOnlyList<Issue> Issues => issues;
        public int ErrorCount { get; private set; }
        public bool HasErrors => ErrorCount > 0;
        public string FirstError { get { foreach (var issue in issues) if (issue.IsError) return issue.Message; return ""; } }
        public void Error(string code, Object context, string field, string problem, string fix) => Add(true, code, context, field, problem, fix);
        public void Warning(string code, Object context, string field, string problem, string fix) => Add(false, code, context, field, problem, fix);
        private void Add(bool error, string code, Object context, string field, string problem, string fix)
        {
            string where = context != null ? context.name : "프로젝트";
            if (context is Component component)
            {
                var path = component.transform.name;
                for (var parent = component.transform.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
                where = component.gameObject.scene.path + " :: " + path;
            }
            issues.Add(new Issue { Code = code, Context = context, IsError = error,
                Message = "[WTF 설정 " + code + "] " + where + " | " + field + ": " + problem + " 수정: " + fix });
            if (error) ErrorCount++;
        }
        public void Publish()
        {
            foreach (var issue in issues)
                if (issue.IsError) Debug.LogError(issue.Message, issue.Context);
                else Debug.LogWarning(issue.Message, issue.Context);
        }
    }
}
