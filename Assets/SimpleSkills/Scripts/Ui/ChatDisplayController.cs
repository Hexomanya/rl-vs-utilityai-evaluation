using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using KBCore.Refs;
using TMPro;
using UnityEngine;

namespace SimpleSkills.Scripts.Ui
{
    public struct ChatMessage
    {
        public string Message;
        [CanBeNull] public ISkAgent Caller;
    }
    
    public class ChatDisplayController : ValidatedMonoBehaviour
    {
        [SerializeField, Self] private TextMeshProUGUI _textField;
        [SerializeField] private int _maxMessages;

        private List<ChatMessage> _textMessages = new List<ChatMessage>();

        private void OnEnable()
        {
            if(_maxMessages < 0) _maxMessages = 0;
            GameLog.OnLogMessage += this.OnLogMessage;
        }
        
        private void OnDisable()
        {
            GameLog.OnLogMessage -= this.OnLogMessage;
        }

        private void OnLogMessage(string message, ISkAgent caller)
        {
            ChatMessage chatMessage = new ChatMessage{
                Message = message,
                Caller = caller,
            };
            
            _textMessages.Add(chatMessage);
            this.RestrictMessageBuffer();

            this.UpdateChatDisplay();
        }
        
        private void RestrictMessageBuffer()
        {
            int excess = _textMessages.Count - _maxMessages;
            if (excess <= 0) return;
            
            _textMessages.RemoveRange(0, excess);
        }

        private void UpdateChatDisplay()
        {
            string text = ChatDisplayController.ComposeAllMessages(_textMessages);
            _textField.text = text;

            _textField.enabled = _textMessages.Count > 0;
        }

        private static string ComposeTextMessage(ChatMessage message)
        {
            string prefixText = message.Caller == null ? "System:" : $"{message.Caller.GetName()}:";
            return $"{prefixText} {message.Message}";
        }

        private static string ComposeAllMessages(List<ChatMessage> messages)
        {
            StringBuilder stringBuilder = new StringBuilder();
        
            foreach (ChatMessage message in messages)
            {
                stringBuilder.AppendLine(ChatDisplayController.ComposeTextMessage(message));
            }
        
            return stringBuilder.ToString();
        }
    }
}
