﻿using System;

namespace Library.API.Models
{
    public class BookDto : LinkedResourceBaseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid AuthorId { get; set; }
    }
}