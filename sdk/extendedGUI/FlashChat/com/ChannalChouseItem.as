package com
{
   import flash.events.MouseEvent;
   import flash.text.TextField;
   import flash.text.TextFormat;
   import scaleform.clik.core.UIComponent;
   import scaleform.clik.data.ListDataSO;
   import scaleform.clik.events.ButtonEvent;
   
   public class ChannalChouseItem extends UIComponent
   {
      public var textField:TextField;
      
      public var defaultTextFormat:TextFormat;
      
      public var comField:TextField;
      
      protected var _stateMap:Object = {
         "up":["up"],
         "over":["over"],
         "down":["down"],
         "notification":["notification"],
         "release":["release","over"],
         "out":["out","up"],
         "disabled":["disabled"],
         "selecting":["selecting","down"],
         "toggle":["toggle","up"],
         "kb_selecting":["kb_selecting","up"],
         "kb_release":["kb_release","out","up"],
         "kb_down":["kb_down","down"]
      };
      
      protected var statesDefault:Vector.<String> = Vector.<String>([""]);
      
      protected var statesSelected:Vector.<String> = Vector.<String>(["selected_",""]);
      
      protected var _newFrame:String;
      
      protected var _State:String;
      
      protected var _state:String;
      
      protected var _index:uint;
      
      protected var _selected:Boolean;
      
      protected var _label:String;
      
      protected var _data:Object;
      
      protected var _mouseDown:int = 0;
      
      protected var _com:String;
      
      protected var _pressedByKeyboard:Boolean = false;
      
      public var allowDeselect:Boolean = true;
      
      protected var _toggle:Boolean = false;
      
      public function ChannalChouseItem()
      {
         super();
         this.textField.alpha = 0.7;
         this.comField.alpha = 0.7;
         addEventListener(MouseEvent.MOUSE_DOWN,this.onClick);
         addEventListener(MouseEvent.MOUSE_OVER,this.handleMouseOver,false,0,true);
         addEventListener(MouseEvent.MOUSE_OUT,this.handleMouseOut,false,0,true);
      }
      
      protected function handleMouseOver(e:MouseEvent) : *
      {
         this.textField.alpha = 1;
         this.comField.alpha = 1;
      }
      
      protected function handleMouseOut(e:MouseEvent) : *
      {
         this.textField.alpha = 0.7;
         this.comField.alpha = 0.7;
      }
      
      public function get index() : uint
      {
         return this._index;
      }
      
      public function set index(value:uint) : void
      {
         this._index = value;
      }
      
      public function get selected() : Boolean
      {
         return this._selected;
      }
      
      public function set selected(value:Boolean) : void
      {
         if(this._selected == value)
         {
            return;
         }
         this._selected = value;
      }
      
      public function setcomField(id:Number, s:String, color:uint) : void
      {
         this._com = s;
         var newFormat:TextFormat = new TextFormat();
         newFormat.color = color;
         if(id != -99)
         {
            this.comField.defaultTextFormat = newFormat;
            this.comField.setTextFormat(newFormat);
            this.comField.text = s;
            this.comField.textColor = color;
         }
         else
         {
            this.comField.text = "";
         }
         this.textField.textColor = color;
         this.textField.defaultTextFormat = newFormat;
         this.textField.setTextFormat(newFormat);
      }
      
      public function get label() : String
      {
         return this._label;
      }
      
      public function set label(value:String) : void
      {
         if(this._label == value)
         {
            return;
         }
         this._label = value;
         this.textField.text = value;
      }
      
      public function setListData(listData:ListDataSO) : void
      {
         this.index = listData.index;
         this.selected = listData.selected;
         this.label = listData.label || "";
         this._State = listData.state || "";
      }
      
      public function setState(s:String) : *
      {
      }
      
      public function setData(data:Object) : void
      {
         this.data = data;
      }
      
      public function get data() : Object
      {
         return this._data;
      }
      
      public function set data(value:Object) : void
      {
         this._data = value;
      }
      
      protected function onClick(e:MouseEvent) : *
      {
         var sfEvent:ButtonEvent = new ButtonEvent(ButtonEvent.CLICK,true,false,this.index,0,true,false);
         dispatchEvent(sfEvent);
      }
   }
}

