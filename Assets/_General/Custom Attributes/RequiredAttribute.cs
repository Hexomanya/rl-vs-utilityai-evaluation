using UnityEngine;

namespace _General.Custom_Attributes
{
    public class RequiredAttribute : PropertyAttribute
    {

        public RequiredAttribute(string errorMessage = "This field is required.")
        {
            this.ErrorMessage = errorMessage;
        }
        public string ErrorMessage { get; private set; }
    }
}
