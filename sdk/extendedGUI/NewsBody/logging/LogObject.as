package logging
{
   public class LogObject
   {
      private static var SPLITTER:String = " ";
      
      public var date:Date;
      
      private var args:Array;
      
      public var channel:String;
      
      public function LogObject(_args:Array, channel:String = null)
      {
         super();
         this.args = _args;
         this.date = new Date();
         this.channel = channel == null ? Logger.DEFAULT : channel;
      }
      
      public function toString() : String
      {
         var temp:String = new String();
         for(var j:int = 0; j < this.args.length; j++)
         {
            if(this.args[j] != null)
            {
               temp += this.args[j].toString() + SPLITTER;
            }
         }
         return temp;
      }
   }
}

