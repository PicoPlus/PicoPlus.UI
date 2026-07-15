namespace NovinCRM.Domain.Webhooks;

/// <summary>
/// The lifecycle verb component of a HubSpot webhook subscription type.
/// Combined with <see cref="HubSpotObjectType"/> to form e.g. "contact.propertyChange".
/// </summary>
public enum HubSpotEventType
{
    Unknown = 0,

    /// <summary>Object record was created.</summary>
    Creation,

    /// <summary>Object record was permanently deleted.</summary>
    Deletion,

    /// <summary>Object record was restored from the recycling bin.</summary>
    Restoration,

    /// <summary>A property value on the object record changed.</summary>
    PropertyChange,

    /// <summary>An association between two objects was added.</summary>
    AssociationChange,

    /// <summary>A privacy deletion request was processed.</summary>
    PrivacyDeletion,

    /// <summary>A merge was performed on the object.</summary>
    Merge
}
