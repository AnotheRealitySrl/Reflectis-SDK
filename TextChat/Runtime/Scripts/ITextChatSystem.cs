using Reflectis.SDK.Core;
using System;
using System.Collections.Generic;

namespace Reflectis.SDK.TextChat
{
    /// <summary>
    /// Manages the text communication among players.
    /// </summary>
    public interface ITextChatSystem : ISystem
    {
        string[] ActiveChatChannels { get; }

        bool IsConnected { get; }

        #region Events

        #region Account events

        /// <summary>
        /// Event invoked when the user connects to the chat
        /// </summary>
        event Action OnUserConnected;

        /// <summary>
        /// Event invoked when the user disconnects to the chat
        /// </summary>
        event Action OnUserDisconnected;

        #endregion

        #region Message events

        /// <summary>
        /// Event invoked when a message is received. 
        /// It will pass the message and the channel in which the message was sent
        /// </summary>
        event Action<ChatMessage, string> OnTxtMsgReceived;

        #endregion

        #region Channel events

        /// <summary>
        /// Event invoked when a user joins a channel
        /// </summary>
        event Action<string> OnJoinedChannel;

        #endregion

        #endregion

        #region Methods
        /// <summary>
        /// Wheter the user can chat or not
        /// </summary>
        /// <returns></returns>
        bool CanChat();
        /// <summary>
        /// Wheter the user can chat or not in the given channel
        /// </summary>
        /// <returns></returns>
        bool CanChatInChannel(string channelId);

        /// <summary>
        /// Disconnect the current user to the text chat
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Sends a new message to a specific user
        /// </summary>
        /// <param name="userId">Id of the user who will receive the message</param>
        /// <param name="msg">A message</param>
        void SendMessageToUser(string userId, ChatMessage msg);

        /// <summary>
        /// Sends a message to a specific channel
        /// </summary>
        /// <param name="channelId">Id of the channel that will receive the message</param>
        /// <param name="msg">A message</param>
        void SendMessageToChannel(string channelId, ChatMessage msg);

        /// <summary>
        /// Get all the public chat rooms
        /// </summary>
        List<ChatRoom> GetPublicRooms();

        /// <summary>
        /// Get all the private channels ids
        /// </summary>
        List<string> GetPrivateChannelIds();

        /// <summary>
        /// Joins a text channel
        /// </summary>
        /// <param name="channelId">Id of the channel</param>
        void JoinTextChannel(string channelId);


        /// <summary>
        /// Get info from a specific room
        /// </summary>
        /// <param name="channelId"></param>
        public ChatRoom GetRoomInfo(string channelId);

        /// <summary>
        /// Leave a text channel
        /// </summary>
        /// <param name="channelId">Id of the channel</param>
        void LeaveTextChannel(string channelId);

        /// <summary>
        /// Delete all messages from a specific conversation.
        /// This method is not supported for PhotonTextChatSystem.
        /// </summary>
        /// <param name="conversationId"></param>
        /// <param name="type"></param>
        //void DeleteConversationFromServer(string conversationId, EChatMessageType type);

        #endregion
    }
}