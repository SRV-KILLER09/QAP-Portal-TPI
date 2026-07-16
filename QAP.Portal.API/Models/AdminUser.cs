namespace QAP.Portal.API.Models
{
    public class AdminUser
    {
        public string ADMIN_ID { get; set; } = string.Empty;

        public string ADMIN_NAME { get; set; } = string.Empty;

        public string EMAIL { get; set; } = string.Empty;

        public string PASSWORD_HASH { get; set; } = string.Empty;

        public string STATUS { get; set; } = string.Empty;

        public DateTime? CREATED_ON { get; set; }
    }
}