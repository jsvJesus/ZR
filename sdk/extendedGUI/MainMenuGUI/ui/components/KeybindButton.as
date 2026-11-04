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
      
      public function KeybindButton(kb:Keybind)
      {
         this._keybind = kb;
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
         var oldWidth:uint = 0;
         var btn:* = undefined;
         var i:uint = 0;
         for(i = 0; i < this._keybind.shortcuts.length; i++)
         {
            btn = this.buttons[i];
            oldWidth = uint(btn.width);
            (btn as PushButton).upColor = 0;
            (btn as PushButton).upColorAlpha = 1;
            switch(String(this._keybind.shortcuts[i]).length)
            {
               case 0:
                  btn.size = 18;
                  btn.width = 35;
                  (btn as PushButton).upColor = emptyColor;
                  (btn as PushButton).upColorAlpha = 0.5;
               case 1:
                  btn.size = 18;
                  btn.width = 35;
                  break;
               case 2:
                  btn.size = 18;
                  btn.width = 45;
                  break;
               case 3:
                  btn.size = 18;
                  btn.width = 50;
                  break;
               case 4:
                  btn.size = 18;
                  btn.width = 60;
                  break;
               case 5:
               case 6:
                  btn.size = 18;
                  btn.width = 65;
                  break;
               case 7:
               case 8:
                  btn.width = 75;
                  btn.size = 15;
               case 9:
               case 10:
                  btn.width = 100;
                  btn.size = 15;
               case 11:
               case 12:
                  btn.width = 110;
                  btn.size = 15;
            }
            btn.enabled = this._keybind.allowEdit;
            if(oldWidth != btn.width)
            {
               btn.alpha = 0;
               TweenMax.killTweensOf(btn);
               TweenMax.to(btn,1.3,{
                  "alpha":1,
                  "ease":Expo.easeOut
               });
            }
         }
      }
      
      protected function onButtonClick(event:MouseEvent) : void
      {
         setTimeout(this.onButtonClickDeals,30,event);
      }
      
      protected function onButtonClickDeals(event:MouseEvent) : void
      {
         Base.navigator.showKeyboardMessage(event.target as PushButton,Locale.getById(this._keybind.localeId));
         this.targetShortcutButton = event.target as PushButton;
         this.targetShortcutIndex = this.helper[this.targetShortcutButton];
         this._keybind.addEventListener(Event.CHANGE,this.onKeybindChange);
         this._keybind.addEventListener(Event.CANCEL,this.onKeybindCancel);
         this._keybind.getNewBind();
      }
      
      protected function onKeybindChange(event:Event) : void
      {
         Base.navigator.hideKeyboardMessage(event.target as PushButton);
         this._keybind.removeEventListener(Event.CHANGE,this.onKeybindChange);
         this._keybind.removeEventListener(Event.CHANGE,this.onKeybindCancel);
         this.targetShortcutButton.label = this._keybind.shortcuts[this.targetShortcutIndex];
         this.dispatchEvent(new Event(Event.CHANGE));
         this.invalidate();
      }
      
      protected function onKeybindCancel(event:Event) : void
      {
         Base.navigator.hideKeyboardMessage(event.target as PushButton);
         this._keybind.removeEventListener(Event.CHANGE,this.onKeybindChange);
         this._keybind.removeEventListener(Event.CHANGE,this.onKeybindCancel);
         this.dispatchEvent(new Event(Event.CHANGE));
         this.invalidate();
      }
      
      override public function draw() : void
      {
         var btn:* = undefined;
         this.graphics.clear();
         this.graphics.beginFill(this.changed ? changedColor : regularColor,this.changed ? changedAlpha : regularAlpha);
         this.graphics.drawRect(0,0,_width,_height);
         this.graphics.endFill();
         this.label.x = 5;
         this.label.y = (_height - this.label.height) / 2;
         this.tuneButtonWidth();
         var i:uint = 0;
         for(i = 0; i < this._keybind.shortcuts.length; i++)
         {
            btn = this.buttons[i];
            btn.x = _width - btn.width;
            btn.height = _height;
         }
      }
   }
}

