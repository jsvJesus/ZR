package ui.screens
{
   import com.dvalimona.components.*;
   import communication.*;
   import events.*;
   import flash.events.*;
   import flash.utils.*;
   import lang.*;
   import logging.*;
   import ui.Screen;
   import ui.components.*;
   
   public class NewCharScreen extends Screen
   {
      protected var vBox:VBox;
      
      protected var linesBox:VBox;
      
      protected var headerPanel:HeaderPanel;
      
      protected var createButton:MenuButton;
      
      protected var backButton:MenuButton;
      
      protected var charNameBox:HBoxLine;
      
      protected var charNameLabel:Label;
      
      protected var charNameInput:InputText;
      
      protected var randomButton:MenuButton;
      
      protected var itemSteppers:Array;
      
      protected var quad:Quad;
      
      protected var nameAllowed:Boolean;
      
      public function NewCharScreen(param1:String, param2:uint = 0)
      {
         super(param1,param2,false);
      }
      
      override protected function unfreeze(... rest) : void
      {
         Dummy.hide();
         this.label = BreadCrumbs.DIV + Locale.getById("extendedGUI.NewCharWindow.newChar");
         super.unfreeze();
         this.clearLinesBox();
         this.charNameInput.text = "";
         this.nameAllowed = false;
         this.updateCreateButton();
         this.charNameBox.draw();
         this.linesBox.draw();
         this.vBox.draw();
         Logger.LogToChannel(Logger.DEBUG,"NewCharScreen.unfreeze");
         setTimeout(Character.StartCharCreating,50,this.onConfigReceived);
      }
      
      override protected function init(... rest) : void
      {
         this.vBox = new VBox();
         this.vBox.x = 50;
         this.vBox.y = 100;
         this.vBox.alignment = VBox.LEFT;
         this.vBox.spacing = 1;
         this.vBox.debug = false;
         super.addChild(this.vBox);
         this.charNameBox = new HBoxLine();
         this.charNameBox.paddingLeft = 0;
         this.charNameBox.setSize(widths[0],40);
         this.charNameLabel = new Label(this.charNameBox.left);
         this.charNameLabel.autoSize = true;
         this.charNameLabel.$ = "extendedGUI.NewCharWindow.charName";
         this.charNameLabel.y = 9;
         this.charNameLabel.size = 20;
         this.charNameLabel.paddingLeft = 10;
         this.charNameInput = new InputText(this.charNameBox.right);
         this.charNameInput.setSize(300,40);
         this.charNameInput.size = 22;
         this.charNameInput.paddingRight = 0;
         this.charNameInput.addEventListener(Event.CHANGE,this.onCharNameChanged);
         this.charNameInput.maxChars = 32;
         this.vBox.addChild(this.charNameBox);
         this.linesBox = new VBox();
         this.linesBox.alignment = VBox.LEFT;
         this.linesBox.debug = false;
         this.linesBox.width = widths[0];
         this.linesBox.spacing = 1;
         this.linesBox.paddingLeft = 0;
         this.linesBox.paddingTop = 10;
         this.vBox.addChild(this.linesBox);
         this.randomButton = new MenuButton(this.vBox);
         this.randomButton.$ = "extendedGUI.NewCharWindow.randomizeChar";
         this.randomButton.height = 35;
         this.randomButton.paddingTop = 10;
         this.randomButton.addEventListener(MouseEvent.CLICK,this.randomClickHandler);
         this.createButton = new MenuButton(this.vBox,0,0,"",this.onCreateButtonHandler);
         this.createButton.$ = "extendedGUI.NewCharWindow.goCreate";
         this.createButton.height = 35;
         this.createButton.enabled = false;
         this.backButton = new MenuButton(this.vBox,0,0,"",this.onBackButtonHandler);
         this.backButton.$ = "extendedGUI.NewCharWindow.backButton";
         this.backButton.height = 35;
         appears = [];
         this.charNameInput.text = "";
         this.defaultFocus = this.charNameInput;
      }
      
      protected function setFocus() : void
      {
         Base.stage.focus = this.charNameInput.textField;
      }
      
      protected function onCharNameChanged(param1:Event) : void
      {
         var event:Event = param1;
         Logger.LogToChannel(Logger.DEBUG,"NewCharScreen.onCharNameChanged");
         try
         {
            this.nameAllowed = false;
            this.updateCreateButton();
            event.stopPropagation();
            Callout.ClearInstances();
            Api.self.removeEventListener(Api.CHECK_AVATAR_NAME,this.onCheckNameHandler);
            Api.self.addEventListener(Api.CHECK_AVATAR_NAME,this.onCheckNameHandler);
            Api.call(Api.CHECK_AVATAR_NAME,[{"nick":this.charNameInput.text}]);
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,error);
         }
      }
      
      protected function onCheckNameHandler(param1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.CHECK_AVATAR_NAME,this.onCheckNameHandler);
         Logger.LogToChannel(Logger.DEBUG,"NewCharScreen.onCheckNameHandler",param1.data.answer,param1.data.error,param1.data.answer.result);
         if(param1.data.answer.result)
         {
            this.nameAllowed = true;
         }
         else
         {
            this.nameAllowed = false;
         }
         this.showNameCallout(param1.data.answer.message);
         this.updateCreateButton();
      }
      
      private function updateCreateButton() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"NewCharScreen.updateCreateButton",this.charNameInput.text.length > 0,this.nameAllowed);
         this.createButton.enabled = this.charNameInput.text.length > 0 && Boolean(this.nameAllowed);
      }
      
      private function showNameCallout(param1:String) : void
      {
         var _loc2_:Callout = new Callout(this.charNameInput,param1,300,50);
      }
      
      protected function randomClickHandler(param1:MouseEvent) : void
      {
         this.randomize();
      }
      
      protected function randomize() : void
      {
         var _loc1_:ItemStepper = null;
         var _loc2_:uint = 0;
         while(_loc2_ < this.itemSteppers.length)
         {
            _loc1_ = this.itemSteppers[_loc2_] as ItemStepper;
            _loc1_.randomize();
            _loc2_++;
         }
      }
      
      protected function onCreateButtonHandler(param1:Event) : void
      {
         Api.self.addEventListener(Api.CREATE_CHAR,this.onCreateCharHandler);
         Api.call(Api.CREATE_CHAR,[]);
      }
      
      protected function onCreateCharHandler(param1:ApiEvent) : void
      {
         if(param1.data.answer.success == 1)
         {
            this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.ROOT_SCREEN));
            setTimeout(Character.Update,1500);
         }
         else
         {
            Base.navigator.showDialog("extendedGUI.Dialogs.Error",param1.data.answer.msg,true,[new DialogButtonItem("extendedGUI.Dialogs.Ok",null,1)]);
         }
      }
      
      protected function onBackButtonHandler(param1:Event) : void
      {
         this.goBack();
      }
      
      override public function goBack() : void
      {
         Character.CancelCharCreating();
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.CHAR_SCREEN));
         BreadCrumbs.Remove(this.breadCrumb);
      }
      
      private function cancelCharCreating() : void
      {
         Character.CancelCharCreating();
      }
      
      private function clearLinesBox() : void
      {
         while(this.linesBox.numChildren > 0)
         {
            this.linesBox.removeChildAt(0);
         }
      }
      
      private function onConfigReceived(param1:Object) : void
      {
         var _loc3_:Object = null;
         var _loc4_:Object = null;
         var _loc5_:Array = null;
         var _loc6_:ItemStepper = null;
         var _loc7_:String = null;
         var _loc8_:String = null;
         Logger.LogToChannel(Logger.DEBUG,"NewCharScreen.onConfigReceived",param1);
         this.clearLinesBox();
         this.itemSteppers = new Array();
         var _loc2_:uint = 0;
         for(_loc7_ in param1)
         {
            _loc5_ = new Array();
            _loc3_ = param1[_loc7_];
            Logger.LogToChannel(Logger.DEBUG,"i:",_loc7_);
            for(_loc8_ in _loc3_)
            {
               _loc4_ = _loc3_[_loc8_];
               _loc4_.ranger = parseInt(_loc8_);
               _loc5_.push(_loc4_);
            }
            _loc6_ = new ItemStepper();
            _loc6_.useDiagridBack = true;
            _loc6_.itemGroup = _loc7_;
            _loc6_.items = _loc5_;
            _loc6_.reverseItems();
            _loc6_.width = widths[0] - 0;
            _loc6_.height = 35;
            _loc6_.addEventListener(Event.CHANGE,this.onItemsChange);
            this.linesBox.addChild(_loc6_);
            this.itemSteppers.push(_loc6_);
            _loc2_++;
         }
         this.charNameBox.draw();
         this.linesBox.draw();
         this.vBox.draw();
         this.randomize();
         this.charNameInput.text = "";
         setTimeout(this.setFocus,0);
         setTimeout(Dummy.show,30);
      }
      
      protected function onItemsChange(param1:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onItemsChange:",param1.target as ItemStepper);
         Api.call(Api.NEW_CHAR_VIEW,[{
            "var":(param1.target as ItemStepper).currentItem,
            "choiceGroup":(param1.target as ItemStepper).itemGroup
         }]);
      }
   }
}

