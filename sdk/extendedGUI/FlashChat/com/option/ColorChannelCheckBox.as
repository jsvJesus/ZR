package com.option
{
   import com.ChannelEvent;
   import com.ChatSettings;
   import com.GameCommunication;
   import com.colorPicker.ColorEvent;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import scaleform.clik.constants.ConstrainMode;
   import scaleform.clik.core.UIComponent;
   import scaleform.clik.data.ListDataChan;
   import scaleform.clik.utils.Constraints;
   
   public class ColorChannelCheckBox extends UIComponent
   {
      public var checkBoxSound:CheckBoxShort;
      
      protected var _color:uint = 0;
      
      protected var _colorR:uint = 0;
      
      protected var _colorG:uint = 0;
      
      protected var _colorB:uint = 0;
      
      protected var _alph:uint = 0;
      
      protected var _silence:Boolean = false;
      
      internal var API:GameCommunication = new GameCommunication();
      
      public var ID:Number = 0;
      
      public var com:String = "";
      
      protected var _index:uint = 0;
      
      protected var _selected:Boolean = false;
      
      public var checkBox:CheckBoxColor;
      
      public var colors:colorView;
      
      public var selected_frame:MovieClip;
      
      public var constraintsDisabled:Boolean = false;
      
      public function ColorChannelCheckBox()
      {
         super();
      }
      
      public function set index(value:int) : *
      {
         this._index = value;
      }
      
      public function get index() : int
      {
         return this._index;
      }
      
      public function set selected(value:Boolean) : *
      {
         this.checkBox.selected = value;
      }
      
      public function get selected() : Boolean
      {
         return this.checkBox.selected;
      }
      
      public function set sound(value:Boolean) : *
      {
         this.checkBoxSound.selected = value;
      }
      
      public function get sound() : Boolean
      {
         return this.checkBoxSound.selected;
      }
      
      public function set enabled1(value:Boolean) : *
      {
         this.checkBox.enabled = value;
      }
      
      public function get enabled1() : Boolean
      {
         return this.checkBox.enabled;
      }
      
      public function set label(value:String) : *
      {
         this.checkBox.label = value;
      }
      
      public function get label() : String
      {
         return this.checkBox.label;
      }
      
      public function set frame(value:Boolean) : *
      {
         this.selected_frame.visible = value;
      }
      
      public function set color(value:uint) : *
      {
         this._color = value;
         this.setcolor(value);
      }
      
      public function get color() : uint
      {
         return this._color;
      }
      
      public function setListData(listData:ListDataChan) : void
      {
         this._silence = true;
         this.index = listData.index;
         this.selected = listData.selected;
         this.label = listData.label || "";
         this.color = listData.color;
         this.enabled1 = listData.enabled;
         this.checkBoxSound.enabled = !ChatSettings.isLocked(listData.id);
         this.ID = listData.id;
         this.com = listData.com;
         this.sound = ChatSettings.getCh(listData.id);
         this._silence = false;
      }
      
      protected function setcolor(color:uint) : *
      {
         this.selected_frame.visible = false;
         this.colors.graphics.clear();
         this.colors.graphics.beginFill(color);
         this.colors.graphics.drawRect(0,0,this.colors.width,this.colors.height);
         this.colors.graphics.endFill();
      }
      
      override protected function preInitialize() : void
      {
         if(!this.constraintsDisabled)
         {
            constraints = new Constraints(this,ConstrainMode.COUNTER_SCALE);
         }
      }
      
      override protected function initialize() : void
      {
         super.initialize();
      }
      
      override protected function configUI() : void
      {
         this.selected_frame.visible = false;
         if(!this.constraintsDisabled)
         {
            constraints.addElement("checkBox",this.checkBox,Constraints.ALL);
            constraints.addElement("colors",this.colors,Constraints.LEFT);
         }
         this.colors.addEventListener(MouseEvent.CLICK,this.onColorClick);
         this.checkBox.addEventListener(Event.SELECT,this.onSelectCheck);
         this.checkBoxSound.addEventListener(Event.SELECT,this.onSoundCheck);
         super.configUI();
      }
      
      protected function saySelectedChan() : *
      {
         dispatchEvent(new ChannelEvent(ChannelEvent.CHANGE,this.index,this.selected,this.ID,this.sound));
      }
      
      protected function onSelectCheck(e:Event) : *
      {
         var MSG:Message = null;
         var f1:Function = null;
         var f2:Function = null;
         if(this.ID == Object(root).MainChat.findSysID() && !this.selected && !this._silence)
         {
            MSG = new Message();
            MSG.name = "Message";
            this.API.modalMode(true);
            f1 = function():*
            {
               Object(root).removeChild(MSG);
               API.modalMode(false);
               saySelectedChan();
            };
            f2 = function():*
            {
               Object(root).removeChild(MSG);
               API.modalMode(false);
               selected = true;
            };
            Object(root).addChild(MSG);
            stage.focus = MSG;
            if(MSG != Object(root).getChildAt(Object(root).numChildren - 2))
            {
               Object(root).swapChildren(MSG,Object(root).getChildAt(Object(root).numChildren - 2));
            }
            MSG.Message1(Object(root).locale.WARNING,Object(root).locale.MSG_SYS,f1,f2,Object(root).locale.YES,Object(root).locale.NO);
         }
         else
         {
            this.saySelectedChan();
         }
      }
      
      protected function onSoundCheck(e:Event) : *
      {
         dispatchEvent(new ChannelEvent(ChannelEvent.CHANGE_SOUND,this.index,this.selected,this.ID,this.sound));
      }
      
      protected function onColorClick(e:MouseEvent) : *
      {
         this.selected_frame.visible = true;
         dispatchEvent(new ColorEvent(ColorEvent.SHOW,this.color,this.index,this.ID));
      }
   }
}

