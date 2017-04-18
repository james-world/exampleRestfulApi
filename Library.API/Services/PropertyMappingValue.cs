using System.Collections;
using System.Collections.Generic;

namespace Library.API.Services
{
    public class PropertyMappingValue
    {
        public IEnumerable<string> DestinationProperties { get; private set; }

        public bool ReverseSort { get; private set; }

        public PropertyMappingValue(IEnumerable<string> destinationProperties, bool reverseSort = false)
        {
            DestinationProperties = destinationProperties;
            ReverseSort = reverseSort;
        }
    }
}