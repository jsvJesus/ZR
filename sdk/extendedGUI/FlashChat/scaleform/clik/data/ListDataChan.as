package scaleform.clik.data
{
   public class ListDataChan extends ListData
   {
      public var color:Number;
      
      public var id:Number;
      
      public var com:String;
      
      public var enabled:Boolean;
      
      public function ListDataChan(index:Number, id:Number, label:String = "Empty", selected:Boolean = false, com:String = "null", color:uint = 0, enabled:Boolean = true)
      {
         this.index = index;
         this.label = label;
         this.selected = selected;
         super(index,label,selected);
         this.color = color;
         this.id = id;
         this.com = com;
         this.enabled = enabled;
      }
      
      override public function toString() : String
      {
         return "[ListDataChan " + index + ", " + label + ", " + selected + ", " + this.color + ", ]";
      }
   }
}

