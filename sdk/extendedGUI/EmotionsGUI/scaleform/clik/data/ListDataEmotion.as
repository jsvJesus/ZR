package scaleform.clik.data
{
   public class ListDataEmotion extends ListData
   {
      public var id:Number;
      
      public var key_name:String;
      
      public var key_id:Number;
      
      public var enabled:Boolean;
      
      public var checked:Boolean;
      
      public function ListDataEmotion(index:Number, id:Number, label:String = "Empty", key_name:String = "", key_id:int = 0, enabled:Boolean = true, checked:Boolean = false)
      {
         this.index = index;
         this.label = label;
         this.key_name = key_name;
         super(index,label,false);
         this.key_id = key_id;
         this.id = id;
         this.enabled = enabled;
         this.checked = checked;
      }
      
      override public function toString() : String
      {
         return "[ListDataEmotion " + index + ", " + label + ", " + this.key_name + ", " + this.key_id + "]";
      }
   }
}

