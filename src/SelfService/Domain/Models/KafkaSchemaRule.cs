using System;

namespace SelfService.Domain.Models;

public class KafkaSchemaRule
{
    public KafkaSchemaRule(
        string name,
        string doc,
        string kind,
        string mode,
        string type,
        List<string> tags,
        KafkaSchemaParams parameters,
        string expr,
        string onSuccess,
        string onFailure,
        bool disabled
    )
    {
        Name = name;
        Doc = doc;
        Kind = kind;
        Mode = mode;
        Type = type;
        Tags = tags;
        Params = parameters;
        Expr = expr;
        OnSuccess = onSuccess;
        OnFailure = onFailure;
        Disabled = disabled;
    }

    public string Name { get; set; }
    public string Doc { get; set; }
    public string Kind { get; set; }
    public string Mode { get; set; }
    public string Type { get; set; }
    public List<string> Tags { get; set; }
    public KafkaSchemaParams Params { get; set; }
    public string Expr { get; set; }
    public string OnSuccess { get; set; }
    public string OnFailure { get; set; }
    public bool Disabled { get; set; }
}
