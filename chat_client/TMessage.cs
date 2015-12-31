using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chat_client
{
    public enum headerCodes
    {
        messageID,
        message,
        sender,
        emoticon,
        deleteID, 
        action
    };

   public class TMessage
    {
       public TMessage()
       {
           msgID = Guid.NewGuid().ToString();
       }
                 
       public string msg { get; set; }

       public string sender { get; set; }
       
       public string header { get; set; }

       public bool read { get; set; }

       public string msgID { get; set; }
    }
}
