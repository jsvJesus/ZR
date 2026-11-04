package ui.components
{
   import flash.display.DisplayObjectContainer;
   
   public class ClothButton extends PictureButton
   {
      protected var _group:String = "";
      
      protected var _description:String = "";
      
      protected var _item_name:String = "";
      
      protected var _item_id:int = 0;
      
      protected var _colors:Array = new Array();
      
      protected var _selectedColor:uint = 0;
      
      protected var _weight:uint = 0;
      
      public function ClothButton(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         super(parent,xpos,ypos);
      }
      
      public function set description(value:String) : *
      {
         this._description = value;
      }
      
      public function get description() : String
      {
         return this._description;
      }
      
      public function set item_name(value:String) : *
      {
         this._item_name = value;
      }
      
      public function get item_name() : String
      {
         return this._item_name;
      }
      
      public function set item_id(value:int) : *
      {
         this._item_id = value;
      }
      
      public function get item_id() : int
      {
         return this._item_id;
      }
      
      public function set selectedColor(value:uint) : *
      {
         this._selectedColor = value;
      }
      
      public function get selectedColor() : uint
      {
         return this._selectedColor;
      }
      
      public function set colors(value:Array) : *
      {
         this._colors = value;
      }
      
      public function get colors() : Array
      {
         return this._colors;
      }
      
      public function set group(value:String) : *
      {
         this._group = value;
      }
      
      public function get group() : String
      {
         return this._group;
      }
      
      public function set weight(value:uint) : *
      {
         this._weight = value;
      }
      
      public function get weight() : uint
      {
         return this._weight;
      }
   }
}

