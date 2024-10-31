using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core_Auth
{
    public class User
    {
        public Guid UniqueId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; } // Assuming hashed or tokenized
        public string PasswordHash { get; set; }
        public DateTime JoinDate { get; set; }
        public DateTime LastActiveDate { get; set; }
        public int ReputationScore { get; set; }
        public int CollaborationScore { get; set; }
        public string Role { get; set; } // Admin, Moderator, Trusted, etc.
        public UserPreferences Preferences { get; set; }
    }

    public class UserPreferences
    {
        public bool IsSelfHosting { get; set; }
    }

    public class UserProfile
    {
        public Guid UserId { get; set; }
        public string DisplayName { get; set; }
        public string Bio { get; set; }
        public string AvatarUrl { get; set; }
        public List<string> SocialLinks { get; set; } = new();
        public UserSettings Settings { get; set; }
    }

    public class UserSettings
    {
        public bool ContentVisibility { get; set; }
        public bool NSFWPreference { get; set; }
    }
}

namespace RepSpace.Core.Community
{
    public class Category
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<string> Rules { get; set; } = new();
        public bool ActiveStatus { get; set; }
    }

    public class Community
    {
        public Guid CommunityId { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastActiveDate { get; set; }
        public int ReputationThreshold { get; set; }
    }

    public class Post
    {
        public Guid PostId { get; set; }
        public Guid AuthorId { get; set; }
        public Guid CategoryId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; } // Text or URL
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class Comment
    {
        public Guid CommentId { get; set; }
        public Guid PostId { get; set; }
        public Guid AuthorId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public Guid? ParentCommentId { get; set; }
    }
}

namespace RepSpace.Core.Moderation
{
    public class Vote
    {
        public Guid VoteId { get; set; }
        public Guid UserId { get; set; }
        public Guid TargetId { get; set; } // FK to Post, Comment, or Proposal
        public string VoteType { get; set; } // Upvote, Downvote, etc.
        public DateTime Timestamp { get; set; }
    }

    public class Proposal
    {
        public Guid ProposalId { get; set; }
        public Guid CreatorId { get; set; }
        public Guid CategoryId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string VoteStatus { get; set; } // Open, Closed, Under Review
        public DateTime? DecisionDate { get; set; }
    }

    public class ModerationAction
    {
        public Guid ModerationActionId { get; set; }
        public Guid ModeratorId { get; set; }
        public Guid TargetId { get; set; } // Post, Comment, User
        public string ActionType { get; set; } // Warn, Ban, Remove Content
        public DateTime Timestamp { get; set; }
        public string Reason { get; set; }
    }
}

namespace RepSpace.Core.Hosting
{
    public class UserHostedContent
    {
        public Guid ContentId { get; set; }
        public Guid UserId { get; set; }
        public string ContentUrl { get; set; }
        public string HostType { get; set; } // Self, Platform, Mirror
        public DateTime CreatedDate { get; set; }
        public DateTime LastAccessedDate { get; set; }
        public string RetentionStatus { get; set; } // Active, Archived, Deleted
    }

    public class MirrorRequest
    {
        public Guid MirrorRequestId { get; set; }
        public Guid ContentId { get; set; }
        public Guid RequestedBy { get; set; }
        public string ApprovalStatus { get; set; } // Pending, Approved, Denied
        public DateTime RequestedDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }
}

namespace RepSpace.Core.Analytics
{
    public class CommunityMetrics
    {
        public Guid MetricId { get; set; }
        public Guid CategoryId { get; set; }
        public string TimePeriod { get; set; } // Weekly, Monthly
        public int TotalPosts { get; set; }
        public int TotalComments { get; set; }
        public int ActiveUsers { get; set; }
        public double GrowthRate { get; set; }
        public double RetentionRate { get; set; }
    }

    public class UserEngagement
    {
        public Guid EngagementId { get; set; }
        public Guid UserId { get; set; }
        public DateTime Date { get; set; }
        public string EngagementType { get; set; } // PostCreation, Vote, Comment
        public int Frequency { get; set; }
        public TimeSpan TimeSpent { get; set; }
    }
}

namespace RepSpace.UI.Web
{
    public class WebUI
    {
        // Handles core UI functions and interactions
        public void RenderHomePage() { /* Code to render the home page */ }
        public void RenderCommunityPage(Guid communityId) { /* Code to display a specific community */ }
    }

    public class CommunityPage
    {
        public Guid CommunityId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public void RenderPosts() { /* Code to display community posts */ }
        public void RenderVotingPanel() { /* Code to display voting options */ }
    }

    public class UserDashboard
    {
        public Guid UserId { get; set; }

        public void RenderProfileOverview() { /* Code to display user profile */ }
        public void RenderReputation() { /* Code to display user's reputation */ }
        public void DisplayUserAchievements() { /* Code to show badges and points */ }
    }
}

namespace RepSpace.UI.User
{
    public class UserProfile
    {
        public Guid UserId { get; set; }
        public string DisplayName { get; set; }
        public string Bio { get; set; }
        public string AvatarUrl { get; set; }

        public void RenderProfilePage() { /* Code to render user's profile page */ }
        public void DisplayReputation() { /* Code to display reputation score */ }
    }

    public class UserSettings
    {
        public Guid UserId { get; set; }
        public bool ContentVisibility { get; set; }
        public bool NSFWPreference { get; set; }

        public void UpdateSettings(bool contentVisibility, bool nsfwPreference)
        {
            // Code to update user settings
            ContentVisibility = contentVisibility;
            NSFWPreference = nsfwPreference;
        }
    }

    public class UserCustomization
    {
        public Guid UserId { get; set; }
        public List<string> Themes { get; set; } = new();
        public List<string> LayoutPreferences { get; set; } = new();

        public void ApplyCustomization(string theme, string layout)
        {
            /* Code to apply customization settings */
        }
    }
}

// Namespace: RepSpace.Utils
