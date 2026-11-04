package ui.components
{
   import com.dvalimona.components.*;
   import com.greensock.*;
   import com.greensock.easing.*;
   import communication.Keybind;
   import flash.events.*;
   import flash.utils.*;
   import lang.*;
   
   public class KeybindButton extends Component
   {
      protected static const regularColor:uint = 16777215;
      
      protected static const changedColor:uint = 16777215;
      
      protected static const emptyColor:uint = 16711680;
      
      protected static const regularAlpha:Number = 0.1;
      
      protected static const changedAlpha:Number = 0.3;
      
      private var _keybind:Keybind;
      
      private var targetShortcutIndex:uint;
      
      private var targetShortcutButton:PushButton;
      
      private var helper:Dictionary;
      
      private var buttons:Array;
      
      private var box:HBox;
      
      private var buttonsBox:HBox;
      
      private var label:LabelShadowed;
      
      private var button:PushButton;
      
      public function KeybindButton(param1:Keybind)
      {
         this._keybind = param1;
         super();
      }
      
      public function get changed() : Boolean
      {
         return this.keybind.changed;
      }
      
      public function get defaulted() : Boolean
      {
         return this.keybind.defaulted;
      }
      
      public function get keybind() : Keybind
      {
         return this._keybind;
      }
      
      public function resetDefault() : void
      {
         this.keybind.resetToDefault();
         this.updateButtonLabel();
      }
      
      override protected function init() : void
      {
         super.init();
      }
      
      override protected function addChildren() : void
      {
         this.helper = new Dictionary();
         this.buttons = new Array();
         this.box = new HBox(this);
         this.box.spacing = 5;
         this.box.alignment = HBox.MIDDLE;
         this.box.horizontalAlign = HBox.NONE;
         this.box.debug = false;
         this.box.backgroundColor = 0;
         this.box.backgroundAlpha = 0.5;
         this.label = new LabelShadowed(this.box);
         this.label.size = 19;
         this.label.autoSize = true;
         this.label.align = Label.LEFT;
         this.label.debug = false;
         this.label.$ = this._keybind.localeId;
         this.button = new PushButton(this.box);
         this.button.focusMarginX = 0;
         this.button.focusMarginY = 0;
         this.buttons.push(this.button);
         this.button.addEventListener(MouseEvent.CLICK,this.onButtonClick);
         this.button.label = this._keybind.shortcuts[0];
         this.button.size = 18;
         this.box.draw();
      }
      
      public function updateButtonLabel() : void
      {
         this.button.label = this._keybind.shortcuts[0];
         this.draw();
      }
      
      protected function tuneButtonWidth() : void
      {
         var _loc1_:uint = 0;
         var _loc3_:* = undefined;
         var _loc2_:uint = 0;
         _loc2_ = 0;
         while(_loc2_ < this._keybind.shortcuts.length)
         {
            _loc3_ = this.buttons[_loc2_];
            _loc1_ = uint(_loc3_.width);
            (_loc3_ as PushButton).upColor = 0;
            (_loc3_ as PushButton).upColorAlpha = 1;
            switch(String(this._keybind.shortcuts[_loc2_]).length)
            {
               case 0:
                  _loc3_.size = 18;
                  _loc3_.width = 35;
                  (_loc3_ as PushButton).upColor = emptyColor;
                  (_loc3_ as PushButton).upColorAlpha = 0.5;
               case 1:
                  _loc3_.size = 18;
                  _loc3_.width = 35;
                  break;
               case 2:
                  _loc3_.size = 18;
                  _loc3_.width = 45;
                  break;
               case 3:
                  _loc3_.size = 18;
                  _loc3_.width = 50;
                  break;
               case 4:
                  _loc3_.size = 18;
                  _loc3_.width = 60;
                  break;
               case 5:
               case 6:
                  _loc3_.size = 18;
                  _loc3_.width = 65;
                  break;
               case 7:
               case 8:
                  _loc3_.width = 75;
                  _loc3_.size = 15;
               case 9:
               case 10:
                  _loc3_.width = 100;
                  _loc3_.size = 15;
               case 11:
               case 12:
                  _loc3_.width = 110;
                  _loc3_.size = 15;
            }
            _loc3_.enabled = this._keybind.allowEdit;
            if(_loc1_ != _loc3_.width)
            {
               _loc3_.alpha = 0;
               TweenMax.killTweensOf(_loc3_);
               TweenMax.to(_loc3_,1.3,{
                  "alpha":1,
                  "ease":Expo.easeOut
               });
            }
            _loc2_++;
         }
      }
      
      protected function onButtonClick(param1:MouseEvent) : void
      {
         setTimeout(this.onButtonClickDeals,30,param1);
      }
      
      protected function onButtonClickDeals(param1:MouseEvent) : void
      {
         Base.navigator.showKeyboardMessage(param1.target as PushButton,Locale.getById(this._keybind.localeId));
         this.targetShortcutButton = param1.target as PushButton;
         this.targetShortcutIndex = this.helper[this.targetShortcutButton];
         this._keybind.addEventListener(Event.CHANGE,this.onKeybindChange);
         this._keybind.addEventListener(Event.CANCEL,this.onKeybindCancel);
         this._keybind.getNewBind();
      }
      
      protected function onKeybindChange(param1:Event) : void
      {
         Base.navigator.hideKeyboardMessage(param1.target as PushButton);
         this._keybind.removeEventListener(Event.CHANGE,this.onKeybindChange);
         this._keybind.removeEventListener(Event.CHANGE,this.onKeybindCancel);
         this.targetShortcutButton.label = this._keybind.shortcuts[this.targetShortcutIndex];
         this.dispatchEvent(new Event(Event.CHANGE));
         this.invalidate();
      }
      
      protected function onKeybindCancel(param1:Event) : void
      {
         Base.navigator.hideKeyboardMessage(param1.target as PushButton);
         this._keybind.removeEventListener(Event.CHANGE,this.onKeybindChange);
         this._keybind.removeEventListener(Event.CHANGE,this.onKeybindCancel);
         this.dispatchEvent(new Event(Event.CHANGE));
         this.invalidate();
      }
      
      override public function draw() : void
      {
         var _loc2_:* = undefined;
         this.graphics.clear();
         this.graphics.beginFill(this.changed ? changedColor : regularColor,this.changed ? changedAlpha : regularAlpha);
         this.graphics.drawRect(0,0,_width,_height);
         this.graphics.endFill();
         this.label.x = 5;
         this.label.y = (_height - this.label.height) / 2;
         this.tuneButtonWidth();
         var _loc1_:uint = 0;
         _loc1_ = 0;
         while(_loc1_ < this._keybind.shortcuts.length)
         {
            _loc2_ = this.buttons[_loc1_];
            _loc2_.x = _width - _loc2_.width;
            _loc2_.height = _height;
            _loc1_++;
         }
      }
   }
}

