﻿using ChatApp.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatApp.Infrastructure.Entities
{
    public class RoomMessageEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int SenderId { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string SenderUsername { get; set; }
        public bool IsStockCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [DataType(DataType.Text)]
        public string Message { get; set; }

        internal RoomMessage ToRoomMessage()
        {
            throw new NotImplementedException();
        }

        public int ChatRoomId { get; set; }
        public ChatRoomEntity ChatRoom { get; set; }
    }
}
