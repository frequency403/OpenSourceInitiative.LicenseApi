namespace OpenSourceInitiative.LicenseApi.Enums;

/// <summary>
///     Enumerates the OSI classification keywords available for licenses.
/// </summary>
public enum OsiLicenseKeyword
{
    /// <summary>
    ///     Popular licenses with strong community support.
    /// </summary>
    PopularStrongCommunity,

    /// <summary>
    ///     Licenses designed for international use or applicability.
    /// </summary>
    International,

    /// <summary>
    ///     Licenses intended for special or niche purposes.
    /// </summary>
    SpecialPurpose,

    /// <summary>
    ///     Licenses that are not intended to be reused for new projects.
    /// </summary>
    NonReusable,

    /// <summary>
    ///     Licenses that have been superseded by newer versions or alternatives.
    /// </summary>
    Superseded,

    /// <summary>
    ///     Licenses that were voluntarily retired by their stewards.
    /// </summary>
    VoluntarilyRetired,

    /// <summary>
    ///     Licenses considered redundant due to more popular alternatives.
    /// </summary>
    RedundantWithMorePopular,

    /// <summary>
    ///     Other miscellaneous classifications.
    /// </summary>
    OtherMiscellaneous,

    /// <summary>
    ///     Licenses that do not fall into any specific category.
    /// </summary>
    Uncategorized
}