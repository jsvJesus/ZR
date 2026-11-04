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
   import flash.utils.*;
   import logging.*;
   
   public class DonatTestWindow extends Window
   {
      protected var textBox:VBox;
      
      protected var discrLabel:Text;
      
      protected var tabStops:Array = [0,120];
      
      protected var buttonsBox:HBox;
      
      protected var buttonsBoxHeight:uint = 60;
      
      protected var okButton:PushButton;
      
      protected var cancelButton:PushButton;
      
      protected var nameAllowed:Boolean;
      
      public function DonatTestWindow(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, title:String = "")
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
         this.discrLabel = new Text();
         this.discrLabel.color = 16777215;
         this.discrLabel.debug = false;
         this.discrLabel.$ = "extendedGUI.DonatTestWindow.text";
         this.discrLabel.size = 18;
         this.discrLabel.font = Base.lightFontName;
         this.discrLabel.selectable = false;
         this.discrLabel.editable = false;
         this.discrLabel.width = 427;
         this.discrLabel.y = 20;
         this.discrLabel.x = 20;
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
         this.cancelButton.$ = "extendedGUI.DonatTestWindow.cancel";
         this.cancelButton.setSize((309 + 151) / 2,this.buttonsBoxHeight);
         this.cancelButton.overColorAlpha = 1;
         this.buttonsBox.addChild(this.cancelButton);
         this.okButton = new PushButton();
         this.okButton.focusMarginX = 0;
         this.okButton.focusMarginY = 0;
         this.okButton.addEventListener(MouseEvent.CLICK,this.onPayClickHandler);
         this.okButton.font = Base.boldFontName;
         this.okButton.size = 22;
         this.okButton.$ = "extendedGUI.DonatTestWindow.pay";
         this.okButton.setSize((309 + 151) / 2,this.buttonsBoxHeight);
         this.okButton.overColorAlpha = 1;
         this.buttonsBox.addChild(this.okButton);
      }
      
      protected function onCancelClickHandler(event:MouseEvent) : void
      {
         Base.navigator.contentShield.hide();
         Auth.self.doLogout();
      }
      
      protected function onPayClickHandler(event:MouseEvent) : void
      {
         Base.navigator.showDialog("extendedGUI.DonatTestWindow.ask_pay_title","extendedGUI.DonatTestWindow.ask_pay",true,[new DialogButtonItem("extendedGUI.DonatTestWindow.ask_decline",this.onFailPay,0.5),new DialogButtonItem("extendedGUI.DonatTestWindow.ask_accept",this.onCreateClickHandler,0.5)]);
      }
      
      protected function onCreateClickHandler() : void
      {
         this.okButton.enabled = false;
         Api.self.removeEventListener(Api.PAY_RESULT,this.onPayHandler);
         Api.self.addEventListener(Api.PAY_RESULT,this.onPayHandler);
         Api.call(Api.TRY_PAY);
      }
      
      protected function onStartGame() : *
      {
         Base.navigator.contentShield.hide();
         if(Character.list.length > 0)
         {
            Base.navigator.showScreen(MainMenuGUI.ROOT_SCREEN);
            setTimeout(Base.navigator.getScreen(MainMenuGUI.ROOT_SCREEN).checkUpdateCharDialog,500);
         }
         else
         {
            Base.navigator.showScreen(MainMenuGUI.CHARNAME_SCREEN);
         }
      }
      
      protected function onPayHandler(event:ApiEvent) : void
      {
         if(int(event.data.answer.result) == 1)
         {
            Base.navigator.showDialog("extendedGUI.DonatTestWindow.ask_pay_title","extendedGUI.DonatTestWindow.answer",true,[new DialogButtonItem("extendedGUI.Dialogs.Ok",this.onStartGame,1)]);
         }
         else
         {
            Base.navigator.showDialog(event.data.answer.title,event.data.answer.message,false,[new DialogButtonItem("extendedGUI.Dialogs.Addgold",this.onAddGoldClick,0.5),new DialogButtonItem("extendedGUI.Dialogs.Ok",this.onFailPay,0.5)]);
         }
      }
      
      protected function onFailPay() : *
      {
         this.okButton.enabled = true;
      }
      
      protected function onAddGoldClick() : *
      {
         this.onFailPay();
         Gold.addGoldOpenURL();
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
      }
      
      protected function disableMe() : void
      {
         TweenMax.killTweensOf(this);
         TweenMax.to(this,0.5,{
            "alpha":0.2,
            "ease":Expo.easeOut
         });
         this.okButton.enabled = false;
      }
      
      private function headerDeals() : void
      {
         header.tabEnabled = false;
         header.tabChildren = true;
         leftItems.shift = 3;
         _titleLabel.autoSize = true;
         _titleLabel.align = TextFormatAlign.CENTER;
         _titleLabel.font = Base.boldFontName;
         _titleLabel.$ = "extendedGUI.DonatTestWindow.title";
         _titleLabel.size = 20;
         _titleLabel.mouseEnabled = false;
         _titleLabel.mouseChildren = false;
      }
      
      override public function draw() : void
      {
         super.draw();
         this.buttonsBox.setSize(width,this.buttonsBoxHeight);
         this.buttonsBox.x = 0;
         this.buttonsBox.y = height - this.buttonsBoxHeight;
      }
   }
}

