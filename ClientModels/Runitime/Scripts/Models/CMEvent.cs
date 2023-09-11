using System;
using System.Collections.Generic;

using UnityEngine;

namespace Reflectis.SDK.ClientModels
{
    [Serializable]
    public class CMEvent
    {
        [SerializeField] private int id;
        [SerializeField] private string title;
        [SerializeField] private string longTitle;
        [SerializeField] private string description;
        [SerializeField] private CMCategory category;
        [SerializeField] private CMCategory subCategory;
        [SerializeField] private DateTime startDateTime;
        [SerializeField] private DateTime endDateTime;
        [SerializeField] private List<CMUser> participants;
        [SerializeField] private int maxParticipants;
        [SerializeField] private CMEnvironment environment;
        [SerializeField] private bool isFeatured;
        [SerializeField] private string featuredThumb;
        [SerializeField] private bool isPublic;
        [SerializeField] private bool isVisible;
        [SerializeField] private List<CMEventPermissionSet> permissions;
        [SerializeField] private bool canJoin;
        [SerializeField] private bool isOwner;
        [SerializeField] private bool isDraft;
        [SerializeField] private List<CMResource> resources;
        [SerializeField] private string shortLink;
        [SerializeField] private bool multiplayer;
        [SerializeField] private List<CMTemplateObj> template;

        public int Id { get => id; set => id = value; }
        public string Title { get => title; set => title = value; }
        public string LongTitle { get => longTitle; set => longTitle = value; }
        public string Description { get => description; set => description = value; }
        public CMCategory Category { get => category; set => category = value; }
        public CMCategory SubCategory { get => subCategory; set => subCategory = value; }
        public DateTime StartDateTime { get => startDateTime; set => startDateTime = value; }
        public DateTime EndDateTime { get => endDateTime; set => endDateTime = value; }
        public List<CMUser> Participants { get => participants; set => participants = value; }
        public int MaxParticipants { get => maxParticipants; set => maxParticipants = value; }
        public CMEnvironment Environment { get => environment; set => environment = value; }
        public bool IsFeatured { get => isFeatured; set => isFeatured = value; }
        public string FeaturedThumb { get => featuredThumb; set => featuredThumb = value; }
        public bool IsPublic { get => isPublic; set => isPublic = value; }
        public bool IsVisible { get => isVisible; set => isVisible = value; }
        public List<CMEventPermissionSet> Permissions { get => permissions; set => permissions = value; }
        public bool CanJoin { get => canJoin; set => canJoin = value; }
        public bool IsOwner { get => isOwner; set => isOwner = value; }
        public bool IsDraft { get => isDraft; set => isDraft = value; }
        public List<CMResource> Resources { get => resources; set => resources = value; }
        public string ShortLink { get => shortLink; set => shortLink = value; }
        public bool Multiplayer { get => multiplayer; set => multiplayer = value; }
        public List<CMTemplateObj> Template { get => template; set => template = value; }
    }

}