package com.controls
{
   import com.events.EmotionEvent;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.text.TextField;
   import flash.text.TextFieldAutoSize;
   import scaleform.clik.controls.Button;
   import scaleform.clik.data.ListDataEmotion;
   import scaleform.clik.utils.Constraints;
   
   public class CheckEmotionBox extends EmotionButton
   {
      public var checkBox:CheckBoxForEmo;
      
      public var editBtn:Button;
      
      public var key:TextField;
      
      public function CheckEmotionBox()
      {
         super();
      }
      
      override public function set index(value:int) : *
      {
         _index = value;
      }
      
      override public function get index() : int
      {
         return _index;
      }
      
      override public function set check(value:Boolean) : *
      {
         this.checkBox.selected = value;
      }
      
      override public function get check() : Boolean
      {
         return this.checkBox.selected;
      }
      
      override public function set selected(value:Boolean) : void
      {
      }
      
      override public function get selected() : Boolean
      {
         return this.checkBox.selected;
      }
      
      override public function get toggle() : Boolean
      {
         return this.checkBox.toggle;
      }
      
      override public function set toggle(value:Boolean) : void
      {
         this.checkBox.toggle = value;
      }
      
      override public function set enabled(value:Boolean) : void
      {
         super.enabled = value;
         mouseChildren = true;
         mouseEnabled = true;
         this.checkBox.enabled = value;
         this.checkBox.visible = value;
         this.key.visible = value;
         if(this.key_name == "")
         {
            this.key.text = "??";
         }
         else
         {
            this.key.text = this.key_name;
         }
      }
      
      override public function get enabled() : Boolean
      {
         return this.checkBox.enabled;
      }
      
      override public function set key_name(value:String) : *
      {
         _key_bind = value;
      }
      
      override public function get key_name() : String
      {
         return _key_bind;
      }
      
      override public function set key_id(value:Number) : *
      {
         _key_id = value;
      }
      
      override public function get key_id() : Number
      {
         return _key_id;
      }
      
      override public function set frame(value:Boolean) : *
      {
         selected_frame.visible = value;
      }
      
      override protected function updateText() : void
      {
         if(_label != null && this.checkBox != null)
         {
            this.checkBox.label = _label;
            if(_key_bind == "")
            {
               this.key.text = "??";
            }
            else
            {
               this.key.text = _key_bind;
            }
         }
      }
      
      override public function validateNow(event:Event = null) : void
      {
         super.validateNow(event);
         this.editBtn.x = this.width - 20;
      }
      
      override protected function draw() : void
      {
         super.draw();
         this.editBtn.x = this.width - 20;
         this.editBtn.y = this.height - 15;
         this.editBtn.scaleX = this.scaleX;
         this.editBtn.scaleY = this.scaleY;
         this.editBtn.width = 15;
         this.editBtn.height = 15;
      }
      
      override public function setListData(listData:ListDataEmotion) : void
      {
         this.index = listData.index;
         label = listData.label || "";
         this.key_name = listData.key_name;
         this.key_id = listData.key_id;
         _id = listData.id;
         this.check = listData.checked;
         this.updateText();
      }
      
      override protected function initialize() : void
      {
         super.initialize();
      }
      
      override protected function configUI() : void
      {
         if(!constraintsDisabled)
         {
            constraints.addElement("key_bind",key_bind,Constraints.RIGHT);
            constraints.addElement("selected_frame",selected_frame,Constraints.ALL);
         }
         this.checkBox.autoSize = TextFieldAutoSize.LEFT;
         this.key.autoSize = TextFieldAutoSize.RIGHT;
         this.editBtn.addEventListener(MouseEvent.CLICK,this.onKeyBindClick,false,1);
         this.key.addEventListener(MouseEvent.CLICK,this.onKeyBindClick,false,1);
         this.key.addEventListener(MouseEvent.MOUSE_OVER,this.onOver,false,1);
         this.key.addEventListener(MouseEvent.MOUSE_OUT,this.onOut,false,1);
         this.checkBox.addEventListener(MouseEvent.CLICK,this.onClick,false,1);
         this.checkBox.constraintsDisabled = true;
         this.editBtn.visible = false;
         mouseChildren = true;
         mouseEnabled = true;
      }
      
      protected function onOver(e:MouseEvent) : *
      {
         this.key.textColor = 16777215;
      }
      
      protected function onOut(e:MouseEvent) : *
      {
         this.key.textColor = 7697781;
      }
      
      protected function onEditClick(e:MouseEvent) : *
      {
      }
      
      override protected function onClick(e:MouseEvent) : *
      {
         dispatchEvent(new EmotionEvent(EmotionEvent.CHECK,this,_id,!this.checkBox.selected,this.index));
      }
      
      override protected function onKeyBindClick(e:MouseEvent) : *
      {
         this.key.text = "...";
         this.key_name = "...";
         this.editBtn.visible = false;
         this.key.visible = true;
         this.updateText();
         dispatchEvent(new EmotionEvent(EmotionEvent.KEY_BIND,this,_id,!this.checkBox.selected,this.index));
      }
   }
}

