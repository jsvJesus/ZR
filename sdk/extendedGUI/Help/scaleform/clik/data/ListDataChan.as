package scaleform.clik.data
{
   public class ListDataChan extends ListData
   {
      public var color:Number;
      
      public var id:Number;
      
      public var com:String;
      
      public var enabled:Boolean;
      
      public function ListDataChan(param1:Number, param2:Number, param3:String = "Empty", param4:Boolean = false, param5:String = "null", param6:uint = 0, param7:Boolean = true)
      {
         this.index = param1;
         this.label = param3;
         this.selected = param4;
         super(param1,param3,param4);
         this.color = param6;
         this.id = param2;
         this.com = param5;
         this.enabled = param7;
      }
      
      override public function toString() : String
      {
         return "[ListDataChan " + index + ", " + label + ", " + selected + ", " + this.color + "]";
      }
   }
}

