using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Library.API.Models
{
    public class BookForCreationDto : IValidatableObject
    {
        [Required(ErrorMessage = "You must provide a title.")]
        [MaxLength(100, ErrorMessage = "Title must be no more than 100 characters.")]
        public string Title { get; set; }
        [MaxLength(500, ErrorMessage = "Description must be no more than 500 characters.")]
        public string Description { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Title == Description)
                yield return new ValidationResult("Title and description must differ.", new [] {"Book"});
        }
    }
}