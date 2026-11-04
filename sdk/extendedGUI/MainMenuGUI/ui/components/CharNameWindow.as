package ui.components
{
   import com.dvalimona.components.*;
   import com.greensock.*;
   import com.greensock.easing.*;
   import communication.*;
   import events.ApiEvent;
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   import flash.text.*;
   import logging.*;
   
   public class CharNameWindow extends Window
   {
      protected var nameBox:HBox;
      
      protected var nameBoxHeight:uint = 50;
      
      protected var nameLabel:Label;
      
      protected var discrLabel:Text;
      
      public var nameInput:InputText;
      
      protected var tabStops:Array = [0,120];
      
      protected var buttonsBox:HBox;
      
      protected var buttonsBoxHeight:uint = 60;
      
      protected var okButton:PushButton;
      
      protected var cancelButton:PushButton;
      
      protected var nameAllowed:Boolean;
      
      public function CharNameWindow(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, title:String = "Window")
      {
         super(parent,xpos,ypos,title);
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         this.headerDeals();
         this.bodyDeals();
      }
      
      private function bodyDeals() : void
      {
         this.nameBox = new HBox();
         this.nameBox.debug = false;
         this.nameBox.alignment = HBox.MIDDLE;
         this.nameBox.horizontalAlign = HBox.LEFT;
         this.nameBox.fixedHeight = this.nameBoxHeight;
         this.nameBox.tabStops = this.tabStops;
         this.nameBox.y = 35;
         super.addChild(this.nameBox);
         this.nameLabel = new Label();
         this.nameLabel.debug = false;
         this.nameLabel.autoSize = true;
         this.nameLabel.align = TextFieldAutoSize.CENTER;
         this.nameLabel.$ = "extendedGUI.NewCharWindow.charName";
         this.nameLabel.size = 20;
         this.nameBox.addChild(this.nameLabel);
         this.nameInput = new InputText();
         this.nameInput.setSize(300,40);
         this.nameInput.size = 22;
         this.nameInput.padding = 0;
         this.nameInput.addEventListener(Event.CHANGE,this.onCharNameChanged);
         this.nameBox.addChild(this.nameInput);
         this.discrLabel = new Text();
         this.discrLabel.color = 16777215;
         this.discrLabel.debug = false;
         this.discrLabel.$ = "extendedGUI.CharNameWindow.Discr";
         this.discrLabel.size = 18;
         this.discrLabel.font = Base.lightFontName;
         this.discrLabel.selectable = false;
         this.discrLabel.editable = false;
         this.discrLabel.width = 300;
         this.discrLabel.y = this.nameBox.y + 60;
         this.discrLabel.x = 140;
         this.addChild(this.discrLabel);
         this.buttonsBox = new HBox();
         this.buttonsBox.alignment = HBox.MIDDLE;
         this.buttonsBox.fixedHeight = this.buttonsBoxHeight;
         this.buttonsBox.fixedWidth = 460;
         this.buttonsBox.horizontalAlign = HBox.LEFT;
         this.buttonsBox.spacing = 1;
         this.buttonsBox.debug = false;
         super.addChild(this.buttonsBox);
         this.cancelButton = new PushButton();
         this.cancelButton.focusMarginX = 0;
         this.cancelButton.focusMarginY = 0;
         this.cancelButton.addEventListener(MouseEvent.CLICK,this.onCancelClickHandler);
         this.cancelButton.font = Base.boldFontName;
         this.cancelButton.size = 22;
         this.cancelButton.$ = "extendedGUI.CharNameWindow.Cancel";
         this.cancelButton.setSize((309 + 151) / 2,this.buttonsBoxHeight);
         this.cancelButton.overColorAlpha = 1;
         this.buttonsBox.addChild(this.cancelButton);
         this.okButton = new PushButton();
         this.okButton.focusMarginX = 0;
         this.okButton.focusMarginY = 0;
         this.okButton.addEventListener(MouseEvent.CLICK,this.onCreateClickHandler);
         this.okButton.font = Base.boldFontName;
         this.okButton.size = 22;
         this.okButton.$ = "extendedGUI.CharNameWindow.Create";
         this.okButton.setSize((309 + 151) / 2,this.buttonsBoxHeight);
         this.okButton.overColorAlpha = 1;
         this.okButton.enabled = false;
         this.buttonsBox.addChild(this.okButton);
         this.updateCreateButton();
      }
      
      protected function onCancelClickHandler(event:MouseEvent) : void
      {
         this.okButton.enabled = false;
         this.cancelButton.enabled = false;
         Auth.self.doLogout();
      }
      
      public function setText(value:String) : *
      {
         this.nameInput.text = value;
      }
      
      protected function onCreateClickHandler(event:MouseEvent) : void
      {
         this.okButton.enabled = false;
         this.cancelButton.enabled = false;
         Base.navigator.showCreateCharScreen(null,this.nameInput.text);
         TweenMax.to(this,1,{
            "alpha":0,
            "delay":0.7,
            "z":0,
            "y":70,
            "x":250,
            "scaleX":0.3,
            "scaleY":0.3,
            "ease":Expo.easeOut
         });
      }
      
      protected function onCharNameChanged(event:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"FirstCharNameScreen.onCharNameChanged");
         try
         {
            this.nameAllowed = false;
            this.updateCreateButton();
            event.stopPropagation();
            Callout.ClearInstances();
            Api.self.removeEventListener(Api.CHECK_AVATAR_NAME,this.onCheckNameHandler);
            Api.self.addEventListener(Api.CHECK_AVATAR_NAME,this.onCheckNameHandler);
            Api.call(Api.CHECK_AVATAR_NAME,[{"nick":this.nameInput.text}]);
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,error);
         }
      }
      
      protected function onCheckNameHandler(event:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.CHECK_AVATAR_NAME,this.onCheckNameHandler);
         Logger.LogToChannel(Logger.DEBUG,"FirstCharNameScreen.onCheckNameHandler",event.data.answer,event.data.error,event.data.answer.result);
         if(event.data.answer.result)
         {
            this.nameAllowed = true;
         }
         else
         {
            this.nameAllowed = false;
         }
         this.showNameCallout(event.data.answer.message);
         this.updateCreateButton();
      }
      
      private function showNameCallout(text:String) : void
      {
         var callout:Callout = new Callout(this.nameInput,text,300,50);
      }
      
      private function updateCreateButton() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"FirstCharNameScreen.updateCreateButton",this.nameInput.text.length > 0,this.nameAllowed);
         this.okButton.enabled = this.nameInput.text.length > 0 && Boolean(this.nameAllowed);
      }
      
      public function restoreMe() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"FirstCharNameScreen.restoreMe");
         this.enabled = true;
      }
      
      override public function set enabled(value:Boolean) : void
      {
         if(value)
         {
            this.enableMe();
         }
         else
         {
            this.disableMe();
         }
      }
      
      protected function enableMe() : void
      {
         TweenMax.killTweensOf(this);
         TweenMax.to(this,0.5,{
            "alpha":1,
            "ease":Expo.easeOut
         });
         this.okButton.enabled = true;
         this.cancelButton.enabled = true;
         this.updateCreateButton();
      }
      
      protected function disableMe() : void
      {
         TweenMax.killTweensOf(this);
         TweenMax.to(this,0.5,{
            "alpha":0.2,
            "ease":Expo.easeOut
         });
         this.okButton.enabled = false;
         this.cancelButton.enabled = false;
      }
      
      private function headerDeals() : void
      {
         header.tabEnabled = false;
         header.tabChildren = true;
         leftItems.shift = 3;
         _titleLabel.autoSize = true;
         _titleLabel.align = TextFormatAlign.CENTER;
         _titleLabel.font = Base.boldFontName;
         _titleLabel.$ = "extendedGUI.CharNameWindow.Title";
         _titleLabel.size = 20;
         _titleLabel.mouseEnabled = false;
         _titleLabel.mouseChildren = false;
      }
      
      override public function draw() : void
      {
         super.draw();
         this.nameBox.x = sideMargin;
         this.nameBox.setSize(width - sideMargin * 2,this.nameBoxHeight);
         this.buttonsBox.setSize(width,this.buttonsBoxHeight);
         this.buttonsBox.x = 0;
         this.buttonsBox.y = height - this.buttonsBoxHeight;
      }
   }
}

