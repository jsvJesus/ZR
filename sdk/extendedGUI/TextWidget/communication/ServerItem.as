package communication
{
   public class ServerItem
   {
      private var data:Object;
      
      public function ServerItem(param1:Object)
      {
         super();
         this.data = param1;
      }
      
      public function get id() : String
      {
         if(Boolean(this.data) && Boolean(this.data.hasOwnProperty("id")))
         {
            return this.data.id;
         }
         return null;
      }
      
      public function get label() : String
      {
         if(Boolean(this.data) && Boolean(this.data.hasOwnProperty("label")))
         {
            return this.data.label;
         }
         return null;
      }
      
      public function get address() : String
      {
         if(Boolean(this.data) && Boolean(this.data.hasOwnProperty("address")))
         {
            return this.data.address;
         }
         return null;
      }
      
      public function get ping() : String
      {
         if(Boolean(this.data) && Boolean(this.data.hasOwnProperty("ping")))
         {
            return this.data.ping;
         }
         return null;
      }
      
      public function get using() : int
      {
         if(Boolean(this.data) && Boolean(this.data.hasOwnProperty("using")))
         {
            return parseInt(this.data.using);
         }
         return -1;
      }
      
      public function get usingLabel() : String
      {
         if(this.using >= 0)
         {
            return this.using + "%";
         }
         return "n/a";
      }
      
      public function get isDevelop() : Boolean
      {
         if(Boolean(this.data) && Boolean(this.data.hasOwnProperty("is_dev_serv")))
         {
            return parseInt(this.data.is_dev_serv) == 1;
         }
         return false;
      }
   }
}

