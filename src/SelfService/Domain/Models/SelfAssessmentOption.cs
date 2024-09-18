namespace SelfService.Domain.Models;

public class SelfAssessmentOption
{
    public SelfAssessmentOption(string selfAssessmentType, string description)
    {
        SelfAssessmentType = selfAssessmentType;
        Description = description;
    }

    public string SelfAssessmentType { get; private set; }
    public string Description { get; private set; }
}
