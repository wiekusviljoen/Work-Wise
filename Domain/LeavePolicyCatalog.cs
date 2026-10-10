namespace LeaveWise.Domain;

/// <summary>
/// Human-readable statutory baseline references. These are deliberately not executable entitlements:
/// coverage, sectoral instruments, collective agreements, contract terms and amendments require review.
/// </summary>
public sealed record LeaveRuleReference(
    string CountryCode,
    string LeaveType,
    string BaselineSummary,
    string ConditionsAndCaveats,
    string LegalReference,
    string SourceTitle,
    string SourceUrl,
    string VersionLabel,
    bool RequiresLegalReview = true);

public static class LeavePolicyCatalog
{
    public const string CatalogVersion = "2026.10-baseline-1";

    public static IReadOnlyList<LeaveRuleReference> All { get; } = new List<LeaveRuleReference>
    {
        new("NA", "Annual leave",
            "At least four consecutive weeks per annual leave cycle. For a fixed workweek this is four times the usual workdays per week (for example, 20 working days for five days per week or 24 for six).",
            "Variable schedules use the average number of days worked per week over the preceding 12 months. Timing, occasional leave and public-holiday treatment need to be considered.",
            "Labour Act 11 of 2007, section 23",
            "Namibia Labour Act, Chapter 3 (consolidated text)",
            "https://namiblii.org/akn/na/act/2007/11/eng@2023-03-15/provision/chp_3",
            "Consolidated text consulted 2026-10-10; legal review required"),

        new("NA", "Sick leave",
            "The Act specifies at least 30 working days in a sick-leave cycle for a usual five-day week and 36 for a six-day week; fewer than five days are calculated pro rata. During the first year, one day is earned for every 26 days worked.",
            "The cycle, first-year treatment, proof of illness, occupational-injury exceptions and variable work schedules must be checked against the full Act and the employee's circumstances.",
            "Labour Act 11 of 2007, section 24",
            "Namibia Labour Act, Chapter 3 (consolidated text)",
            "https://namiblii.org/akn/na/act/2007/11/eng@2023-03-15/provision/chp_3",
            "Consolidated text consulted 2026-10-10; legal review required"),

        new("NA", "Compassionate leave",
            "Five working days with full remuneration during each 12-month period of continuous employment for a death or serious illness in the defined family.",
            "The statutory definition of family and any prescribed application requirements must be checked. This is not the same category as South African family-responsibility leave.",
            "Labour Act 11 of 2007, section 25",
            "Namibia Labour Act, Chapter 3 (consolidated text)",
            "https://namiblii.org/akn/na/act/2007/11/eng@2023-03-15/provision/chp_3",
            "Consolidated text consulted 2026-10-10; legal review required"),

        new("NA", "Maternity leave",
            "The Act provides at least 12 weeks of maternity leave after six months of continuous service, subject to the statutory conditions.",
            "Timing, certificates, remuneration and Social Security Commission benefits are separate questions and must not be reduced to a simple paid-days balance.",
            "Labour Act 11 of 2007, sections 26–27",
            "Namibia Labour Act, Chapter 3 (consolidated text)",
            "https://namiblii.org/akn/na/act/2007/11/eng@2023-03-15/provision/chp_3",
            "Consolidated text consulted 2026-10-10; legal review required"),

        new("ZA", "Annual leave",
            "At least 21 consecutive days per annual leave cycle, or by agreement one day for every 17 days worked or entitled to be paid, or one hour for every 17 hours worked or entitled to be paid.",
            "The Act generally covers employees who work at least 24 hours per month. Public holidays, the annual leave cycle and agreements affect how the rule is applied.",
            "Basic Conditions of Employment Act 75 of 1997, sections 19–21",
            "South African Department of Employment and Labour — BCEA",
            "https://www.labour.gov.za/DocumentCenter/Acts/Basic%20Conditions%20of%20Employment/Act%20-%20Basic%20Conditions%20of%20Employment.pdf",
            "Statutory baseline; review status required"),

        new("ZA", "Sick leave",
            "During a 36-month sick-leave cycle, paid sick leave is generally equal to the days the employee would normally work over six weeks. During the first six months, one day is available for every 26 days worked.",
            "Proof-of-incapacity rules, occupational injuries and other exceptions apply. Shift patterns and pay calculations require context.",
            "Basic Conditions of Employment Act 75 of 1997, sections 22–24",
            "South African Department of Employment and Labour — BCEA",
            "https://www.labour.gov.za/DocumentCenter/Acts/Basic%20Conditions%20of%20Employment/Act%20-%20Basic%20Conditions%20of%20Employment.pdf",
            "Statutory baseline; review status required"),

        new("ZA", "Family responsibility leave",
            "Generally three days of paid leave per annual leave cycle for specified events, including the birth or illness of an employee's child or certain family deaths.",
            "The BCEA specifies eligibility conditions including more than four months' employment and working at least four days per week. The qualifying relationship and event matter.",
            "Basic Conditions of Employment Act 75 of 1997, section 27",
            "South African Department of Employment and Labour — Family Responsibility Leave guide",
            "https://www.labour.gov.za/DocumentCenter/Pages/Basic-Guide-to-Family-Responsibility-Leave.aspx",
            "Statutory baseline; review status required"),

        new("ZA", "Parental leave",
            "The Constitutional Court's interim framework changed how parental leave provisions are to be read. A single parent or the only employed party in a parental relationship is entitled to at least four consecutive months; where both parties are employed, the aggregate entitlement is four months and ten days to be shared.",
            "Do not use the old birth-mother / paternity / adoption categories as a simple fixed-day calculator. Family structure, adoption or commissioning arrangements, the interim order and later legislation require specialist review.",
            "Werner van Wyk and Others v Minister of Employment and Labour, [2025] ZACC 20",
            "Constitutional Court of South Africa — judgment and order",
            "https://www.concourt.org.za/index.php/judgement/617-a-werner-van-wyk-and-others-v-minister-for-employment-and-labour-b-commission-for-gender-equality-and-another-v-minister-for-employment-and-labour-and-others",
            "Interim constitutional framework; review status required")
    };

    public static IReadOnlyList<LeaveRuleReference> ForCountry(string countryCode) =>
        All.Where(x => x.CountryCode.Equals(countryCode, StringComparison.OrdinalIgnoreCase)).ToArray();
}
