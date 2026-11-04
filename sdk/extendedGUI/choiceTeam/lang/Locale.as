package lang
{
   import logging.Logger;
   
   public class Locale
   {
      public var caption:String;
      
      public var id:String;
      
      public var path:String;
      
      public var icon:String;
      
      public var resource:Object;
      
      public function Locale(dataPack:Object)
      {
         super();
         try
         {
            this.caption = dataPack.caption;
            this.id = dataPack.id;
            this.path = dataPack.path;
            this.icon = dataPack.icon;
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,error.toString());
            throw error;
         }
      }
      
      public function getById(id:String) : String
      {
         var path:Array = id.split(".");
         trace(this,"getById",id,path,"[" + this.resource + "]");
         return this.resource[path[0]][path[1]];
      }
   }
}

