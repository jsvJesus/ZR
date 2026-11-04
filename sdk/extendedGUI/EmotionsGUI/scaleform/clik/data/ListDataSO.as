package scaleform.clik.data
{
   public class ListDataSO extends ListData
   {
      public var state:String = "";
      
      public function ListDataSO(index:uint, label:String = "Empty", selected:Boolean = false, state:String = "")
      {
         this.index = index;
         this.label = label;
         this.selected = selected;
         super(index,label,selected);
         this.state = state;
      }
      
      override public function toString() : String
      {
         return "[ListDataSO " + index + ", " + label + ", " + selected + ", " + this.state + "]";
      }
   }
}

