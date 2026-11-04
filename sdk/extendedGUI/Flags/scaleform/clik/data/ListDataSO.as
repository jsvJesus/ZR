package scaleform.clik.data
{
   public class ListDataSO extends ListData
   {
      public var state:String = "";
      
      public function ListDataSO(param1:uint, param2:String = "Empty", param3:Boolean = false, param4:String = "")
      {
         this.index = param1;
         this.label = param2;
         this.selected = param3;
         super(param1,param2,param3);
         this.state = param4;
      }
      
      override public function toString() : String
      {
         return "[ListDataSO " + index + ", " + label + ", " + selected + ", " + this.state + "]";
      }
   }
}

