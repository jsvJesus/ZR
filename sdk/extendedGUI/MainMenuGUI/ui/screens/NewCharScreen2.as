package ui.screens
{
   import com.colorPicker.*;
   import com.dvalimona.components.*;
   import communication.*;
   import events.*;
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   import flash.system.*;
   import flash.utils.*;
   import lang.*;
   import logging.*;
   import ui.Screen;
   import ui.components.*;
   
   public class NewCharScreen2 extends Screen
   {
      protected var controlBox:VBox;
      
      protected var faceControlBox:VBox;
      
      protected var clothControlBox:VBox;
      
      protected var clothAdditionalBox:VBox;
      
      protected var faceAdditionalBox:VBox;
      
      protected var SCREEN_PERCENT:Number = 0.245625;
      
      protected var CHANGE_FACE_GOLD_STRING:String = "";
      
      protected var headerPanel:HeaderPanel;
      
      protected var charNameBox:HBox;
      
      protected var charNameLabel:Label;
      
      public var charNameInput:InputText;
      
      protected var createButton:MenuButton2;
      
      protected var backButton:MenuButton2;
      
      protected var faceButton:MenuButton2;
      
      protected var clothButton:MenuButton2;
      
      protected var resetButton:MenuButton2;
      
      protected var clothResetButton:MenuButton2;
      
      protected var faceResetButton:MenuButton2;
      
      protected var faceSaveButton:MenuButton2;
      
      protected var faceLoadButton:MenuButton2;
      
      protected var faceBackButton:MenuButton2;
      
      protected var randomButton:MenuButton2;
      
      protected var itemGroups:Array;
      
      protected var faceGroups:Array;
      
      protected var itemDict:Object;
      
      protected var faceDict:Object;
      
      protected var quad:Quad;
      
      protected var current_mode:String = "main";
      
      protected var isFirstChar:Boolean = false;
      
      protected var isOldChar:Boolean = false;
      
      protected var nameAllowed:Boolean;
      
      protected var _savedFace:Array;
      
      protected var _savedHead:Array = new Array();
      
      protected var _savedCloth:Array = new Array();
      
      protected var _faceMode:Boolean = false;
      
      protected var _donat_only_face:Boolean = false;
      
      protected var _create_flag_nick:Boolean = false;
      
      protected var _create_flag_python:Boolean = false;
      
      protected var _change_flag:Boolean = false;
      
      public function NewCharScreen2(id:String, depth:uint = 0)
      {
         super(id,depth,false);
      }
      
      public function setFirstChar(value:Boolean) : *
      {
         this.isFirstChar = value;
         if(this.isFirstChar)
         {
            this.backButton.$ = "extendedGUI.RootWindow.quitButton";
         }
         else
         {
            this.backButton.$ = "extendedGUI.NewCharWindow.backButton";
         }
      }
      
      public function setOldChar(value:Boolean) : *
      {
         this.isOldChar = value;
         this.charNameInput.enabled = !value;
         if(this.isOldChar)
         {
            this.createButton.$ = "extendedGUI.NewCharWindow.goUpdate";
            this.label = BreadCrumbs.DIV + Locale.getById("extendedGUI.NewCharWindow.updateChar");
         }
         else
         {
            this.createButton.$ = "extendedGUI.NewCharWindow.goCreate";
            this.label = BreadCrumbs.DIV + Locale.getById("extendedGUI.NewCharWindow.newChar");
         }
         BreadCrumbs.Change();
      }
      
      public function setDonatChangeFace(value:Boolean) : *
      {
         this._donat_only_face = value;
         if(value)
         {
            this.setOldChar(value);
         }
         this.clothButton.enabled = !value;
         this._change_flag = false;
      }
      
      override protected function unfreeze(... args) : void
      {
         Dummy.hide();
         if(this.isOldChar)
         {
            this.label = BreadCrumbs.DIV + Locale.getById("extendedGUI.NewCharWindow.updateChar");
         }
         else
         {
            this.label = BreadCrumbs.DIV + Locale.getById("extendedGUI.NewCharWindow.newChar");
         }
         super.unfreeze();
         this.clearBox(this.clothAdditionalBox);
         this.charNameInput.text = "";
         this.nameAllowed = false;
         this.updateCreateButton();
         this.charNameBox.draw();
         this.clothAdditionalBox.draw();
         this.controlBox.draw();
         Logger.LogToChannel(Logger.DEBUG,"NewCharScreen.unfreeze");
         setTimeout(this.StartCharCreating,50);
      }
      
      private function StartCharCreating() : *
      {
         if(this._donat_only_face)
         {
            Character.StartCharDonateFaceEdit(this.onConfigDonateChangeFaceRecived,this.onChangeFaceCostRecived);
         }
         else if(this.isOldChar)
         {
            Character.StartCharUpdating(this.onConfigReceived,this.onCongigFaceReceived);
         }
         else
         {
            Character.StartCharCreating(this.onConfigReceived,this.onCongigFaceReceived);
         }
         this.controlBox.visible = true;
         this.charNameBox.visible = true;
      }
      
      override protected function init(... args) : void
      {
         this.controlBox = new VBox();
         this.controlBox.x = 30;
         this.controlBox.y = 100;
         this.controlBox.alignment = VBox.LEFT;
         this.controlBox.spacing = 1;
         this.controlBox.debug = false;
         super.addChild(this.controlBox);
         this.clothControlBox = new VBox();
         this.clothControlBox.x = 30;
         this.clothControlBox.y = 100;
         this.clothControlBox.alignment = VBox.LEFT;
         this.clothControlBox.spacing = 1;
         this.clothControlBox.debug = false;
         super.addChild(this.clothControlBox);
         this.faceControlBox = new VBox();
         this.faceControlBox.x = 30;
         this.faceControlBox.y = 100;
         this.faceControlBox.alignment = VBox.LEFT;
         this.faceControlBox.spacing = 1;
         this.faceControlBox.debug = false;
         super.addChild(this.faceControlBox);
         this.clothAdditionalBox = new VBox();
         this.clothAdditionalBox.x = Base.stage.stageWidth - Base.stage.stageWidth * this.SCREEN_PERCENT - 30;
         this.clothAdditionalBox.y = 100;
         this.clothAdditionalBox.alignment = VBox.LEFT;
         this.clothAdditionalBox.spacing = 1;
         this.clothAdditionalBox.debug = false;
         this.clothAdditionalBox.width = Base.stage.stageWidth * this.SCREEN_PERCENT;
         super.addChild(this.clothAdditionalBox);
         this.faceAdditionalBox = new VBox();
         this.faceAdditionalBox.x = Base.stage.stageWidth - Base.stage.stageWidth * this.SCREEN_PERCENT - 30;
         this.faceAdditionalBox.y = 100;
         this.faceAdditionalBox.alignment = VBox.LEFT;
         this.faceAdditionalBox.spacing = 1;
         this.faceAdditionalBox.debug = false;
         super.addChild(this.faceAdditionalBox);
         this.charNameBox = new HBox(this.controlBox);
         this.charNameBox.alignment = HBox.MIDDLE;
         this.charNameBox.debug = false;
         this.charNameBox.fixedHeight = 66;
         this.charNameBox.fixedWidth = Base.stage.stageWidth * this.SCREEN_PERCENT;
         this.charNameBox.horizontalAlign = HBox.LEFT;
         this.charNameBox.spacing = 0;
         this.charNameBox.backgroundAlpha = 1;
         this.charNameBox.backgroundColor = 0;
         this.charNameBox.paddingLeft = 0;
         this.charNameBox.paddingTop = 10;
         this.charNameLabel = new Label(this.charNameBox);
         this.charNameLabel.autoSize = false;
         this.charNameLabel.$ = "extendedGUI.NewCharWindow.charName";
         this.charNameLabel.y = 9;
         this.charNameLabel.size = 18;
         this.charNameLabel.paddingLeft = 13;
         this.charNameLabel.width = Base.stage.stageWidth * this.SCREEN_PERCENT * 1 / 3 - this.charNameLabel.paddingLeft;
         this.charNameInput = new InputText(this.charNameBox);
         this.charNameInput.setSize(Base.stage.stageWidth * this.SCREEN_PERCENT * 2 / 3 - this.charNameLabel.paddingLeft,40);
         this.charNameInput.size = 20;
         this.charNameInput.paddingRight = 0;
         this.charNameInput.addEventListener(Event.CHANGE,this.onCharNameChanged);
         this.charNameInput.maxChars = 32;
         this.charNameInput.text = "";
         this.controlBox.addChild(this.charNameBox);
         this.faceButton = new MenuButton2(this.controlBox,0,0,"",this.onFaceButtonHandler);
         this.faceButton.$ = "extendedGUI.NewCharWindow.personalizeFace";
         this.faceButton.autoWidthPercentScreen = this.SCREEN_PERCENT;
         this.faceButton.height = 35;
         this.faceButton.paddingTop = 13;
         this.faceButton.marginLeft = 13;
         this.faceButton.enabled = false;
         this.clothButton = new MenuButton2(this.controlBox,0,0,"",this.onClothButtonHandler);
         this.clothButton.$ = "extendedGUI.NewCharWindow.chooseClothes";
         this.clothButton.autoWidthPercentScreen = this.SCREEN_PERCENT;
         this.clothButton.height = 35;
         this.clothButton.marginLeft = 13;
         this.clothButton.enabled = false;
         this.resetButton = new MenuButton2(this.controlBox);
         this.resetButton.$ = "extendedGUI.NewCharWindow.reset";
         this.resetButton.autoWidthPercentScreen = this.SCREEN_PERCENT;
         this.resetButton.height = 35;
         this.resetButton.enabled = false;
         this.resetButton.marginLeft = 13;
         this.resetButton.enabled = false;
         this.resetButton.addEventListener(MouseEvent.CLICK,this.resetClickHandler);
         this.randomButton = new MenuButton2(this.controlBox);
         this.randomButton.$ = "extendedGUI.NewCharWindow.randomizeChar";
         this.randomButton.autoWidthPercentScreen = this.SCREEN_PERCENT;
         this.randomButton.height = 35;
         this.randomButton.marginLeft = 13;
         this.randomButton.enabled = false;
         this.randomButton.addEventListener(MouseEvent.CLICK,this.randomClickHandler);
         this.quad = new Quad(this.controlBox);
         this.quad.paddingLeft = 13;
         this.quad.width = Base.stage.stageWidth * this.SCREEN_PERCENT - this.quad.paddingLeft;
         this.quad.height = 20;
         this.createButton = new MenuButton2(this.controlBox,0,0,"",this.onCreateButtonHandler);
         this.createButton.$ = "extendedGUI.NewCharWindow.goCreate";
         this.createButton.autoWidthPercentScreen = this.SCREEN_PERCENT;
         this.createButton.height = 35;
         this.createButton.enabled = false;
         this.createButton.marginLeft = 13;
         this.backButton = new MenuButton2(this.controlBox,0,0,"",this.onBackButtonHandler);
         this.backButton.$ = "extendedGUI.NewCharWindow.backButton";
         this.backButton.autoWidthPercentScreen = this.SCREEN_PERCENT;
         this.backButton.height = 35;
         this.backButton.marginLeft = 13;
         this.backButton.enabled = false;
         appears = [];
         Api.self.addEventListener(Api.SET_FACE_FORM,this.onSetFaceFirst);
         Api.self.addEventListener(Api.SET_FACE_VALUES,this.setFaceValue);
         Api.self.addEventListener(Api.GET_ACCESS_LEVEL,this.onAccessLevelChange);
         Api.self.addEventListener(Api.GET_RANDOM_PERSONALITY,this.onSendToPythonRandomFace);
         Api.self.addEventListener(Api.LOCK_CHAR_BUTTONS,this.onLockCharButtons);
         Api.self.addEventListener(Api.ON_FACE_CHANGED,this.onFaceChanged);
         this.defaultFocus = this.charNameInput;
      }
      
      protected function setFocus() : void
      {
         Base.stage.focus = this.charNameInput.textField;
      }
      
      protected function setFaceValue(event:ApiEvent) : void
      {
         var i:int = 0;
         var key:String = null;
         var j:uint = 0;
         var data:Array = event.data.answer.data;
         for(i in data)
         {
            key = data[i]["choiceGroup"];
            if(this.faceDict["data"][key] != null)
            {
               switch(this.getClassName(this.faceDict["data"][key]))
               {
                  case "Array":
                     for(j in this.faceDict["data"][key])
                     {
                        this.faceDict["data"][key][j].selected = this.faceDict["data"][key][j].index == data[i]["value"];
                     }
                     break;
                  case "HUISlider":
                     this.faceDict["data"][key].valueSilence(data[i]["value"]);
                     break;
                  case "ColorList":
                     (this.faceDict["data"][key] as ColorList).setColor(data[i]["value"]);
               }
            }
         }
         if(event.data.answer.save != null && Boolean(event.data.answer.save))
         {
            this._savedHead = data;
         }
      }
      
      protected function onCharNameChanged(event:Event) : void
      {
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
      
      protected function onCheckNameHandler(event:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.CHECK_AVATAR_NAME,this.onCheckNameHandler);
         Logger.LogToChannel(Logger.DEBUG,"NewCharScreen.onCheckNameHandler",event.data.answer,event.data.error,event.data.answer.result);
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
      
      private function updateCreateButton() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"NewCharScreen.updateCreateButton",this.charNameInput.text.length > 0,this.nameAllowed);
         this.createButton.enabled = this.charNameInput.text.length > 0 && Boolean(this.nameAllowed) && Boolean(this._create_flag_python);
         if(this.isOldChar)
         {
            this.createButton.enabled = Boolean(this.createButton.enabled) && Boolean(this._change_flag);
         }
      }
      
      public function createButtonEnabled(value:Boolean) : void
      {
         this.createButton.enabled = value;
         this.nameAllowed = value;
      }
      
      public function resetButtonEnabled() : void
      {
         this.resetButton.enabled = false;
      }
      
      private function showNameCallout(text:String) : void
      {
         var callout:Callout = new Callout(this.charNameInput,text,300,50);
      }
      
      protected function randomClickHandler(event:MouseEvent) : void
      {
         if(!this._donat_only_face)
         {
            this.clothRandomClickHandler(event);
         }
         this.facerandomClickHandler(event);
      }
      
      protected function clothRandomClickHandler(event:MouseEvent) : *
      {
         this.clothResetButton.enabled = true;
         this.resetButton.enabled = true;
         this.randomize();
      }
      
      protected function facerandomClickHandler(event:MouseEvent) : *
      {
         this.faceResetButton.enabled = true;
         this.resetButton.enabled = true;
         this.faceRandomize(false,false,!this.clothButton.enabled);
      }
      
      protected function resetClickHandler(event:MouseEvent) : void
      {
         this.resetButton.enabled = false;
         this.clothresetClickHandler(null);
         this.faceresetClickHandler(null);
      }
      
      protected function faceresetBackHandler(event:MouseEvent) : void
      {
         this.faceresetClickHandler(null);
         this.goBack();
      }
      
      protected function clothresetClickHandler(event:MouseEvent) : void
      {
         if(this.clothResetButton == null)
         {
            return;
         }
         this.clothResetButton.enabled = false;
         Api.call(Api.NEW_FULL_CHAR_VIEW,[this._savedCloth]);
         this.onSetCharView(this._savedCloth);
      }
      
      protected function faceSaveClickHandler(event:MouseEvent) : void
      {
         Api.call(Api.FACE_SAVE_CLICK);
      }
      
      protected function faceLoadClickHandler(event:MouseEvent) : void
      {
         Api.call(Api.FACE_LOAD_CLICK);
      }
      
      protected function faceresetClickHandler(event:MouseEvent) : void
      {
         this.faceResetButton.enabled = false;
         var data:Array = new Array();
         data = data.concat(this._savedHead);
         if(this._savedFace != null)
         {
            data.push({
               "value":this._savedFace,
               "choiceGroup":"FaceForm"
            });
         }
         Api.call(Api.FULL_FACE_CHAR_VIEW,[data]);
         this.onSetFaceView(this._savedHead);
         this._change_flag = false;
      }
      
      protected function onSetCharView(data:Array) : *
      {
         var i:uint = 0;
         var group_name:String = null;
         var color:uint = 0;
         var item_id:int = 0;
         var buttons:Array = null;
         var selected_button:ClothButton = null;
         var j:uint = 0;
         for(i in data)
         {
            group_name = data[i]["choiceGroup"];
            color = uint(data[i]["var"]["item_id"]);
            item_id = int(data[i]["var"]["item_id"]);
            buttons = this.itemDict[group_name]["buttons"];
            for(j in buttons)
            {
               if(buttons[j].item_id == item_id)
               {
                  buttons[j].selected = true;
                  selected_button = buttons[j];
               }
               else
               {
                  buttons[j].selected = false;
               }
            }
            this.itemDict[group_name]["colors"].setData(selected_button.colors);
            this.itemDict[group_name]["item_name"].text = selected_button.item_name;
            this.itemDict[group_name]["descr"].text = selected_button.description;
            selected_button.selectedColor = color;
            this.itemDict[group_name]["colors"].setColor(color);
         }
      }
      
      protected function onSetFaceView(data:Array) : *
      {
         var i:int = 0;
         var key:String = null;
         var j:uint = 0;
         var selected_button:ClothButton = null;
         for(i in data)
         {
            key = data[i]["choiceGroup"];
            switch(this.getClassName(this.faceDict["data"][key]))
            {
               case "Array":
                  for(j in this.faceDict["data"][key])
                  {
                     if(this.faceDict["data"][key][j].index == data[i]["value"])
                     {
                        selected_button = this.faceDict["data"][key][j];
                        selected_button.selected = true;
                     }
                     else
                     {
                        this.faceDict["data"][key][j].selected = false;
                     }
                  }
                  break;
               case "HUISlider":
                  this.faceDict["data"][key].valueSilence(data[i]["value"]);
                  break;
               case "ColorList":
                  (this.faceDict["data"][key] as ColorList).setColor(data[i]["value"]);
                  break;
            }
         }
      }
      
      protected function onSetFaceFirst(event:ApiEvent) : *
      {
         this._savedFace = event.data.answer.data;
      }
      
      private function getRandomElementOf(array:Array) : Object
      {
         var idx:int = Math.floor(Math.random() * array.length);
         return array[idx];
      }
      
      public function getRandomElementWithWeight(array:Array, mode:String = "normal") : Object
      {
         var i:* = undefined;
         var current_chance:Number = Math.random() * 100;
         var selected_indexes:Array = new Array();
         var _tmp_chances:Array = new Array();
         for(i in array)
         {
            _tmp_chances.push(array[i].weight);
         }
         switch(mode)
         {
            case "over":
               for(i in array)
               {
                  if(exception.indexOf(array[i]) != -1)
                  {
                     _tmp_chances[i] += 50;
                  }
               }
               break;
            case "below":
               for(i in array)
               {
                  if(exception.indexOf(array[i]) != -1)
                  {
                     _tmp_chances[i] = 0;
                  }
               }
         }
         for(i in _tmp_chances)
         {
            if(_tmp_chances[i] >= current_chance)
            {
               selected_indexes.push(i);
            }
         }
         if(mode != "normal")
         {
         }
         var selected_index:Number = Math.floor(Math.random() * selected_indexes.length);
         selected_index = Number(selected_indexes[selected_index]);
         selected_indexes = null;
         _tmp_chances = null;
         System.gc();
         return array[selected_index];
      }
      
      protected function randomize(first:Boolean = false) : void
      {
         var buttons:Array = null;
         var i:uint = 0;
         var group_name:String = null;
         var j:uint = 0;
         var selected_button:ClothButton = null;
         var selected_color:uint = 0;
         var obj:Object = null;
         var data:Array = new Array();
         var args:Array = new Array();
         for(i in this.itemGroups)
         {
            group_name = this.itemGroups[i];
            buttons = this.itemDict[group_name]["buttons"];
            for(j in buttons)
            {
               buttons[j].selected = false;
            }
            selected_button = this.getRandomElementOf(buttons);
            selected_button.selected = true;
            this.itemDict[group_name]["colors"].setData(selected_button.colors);
            selected_color = uint(this.itemDict[group_name]["colors"].getRandomColor());
            this.itemDict[group_name]["item_name"].text = selected_button.item_name;
            this.itemDict[group_name]["descr"].text = selected_button.description;
            selected_button.selectedColor = selected_color;
            this.itemDict[group_name]["colors"].setColor(selected_color);
            obj = new Object();
            obj["item_id"] = selected_button.item_id;
            obj["color"] = selected_color;
            data.push({
               "var":obj,
               "choiceGroup":group_name
            });
         }
         if(first)
         {
            this._savedCloth = data;
         }
         args.push(data);
         args.push(first);
         this._change_flag = true;
         Api.call(Api.NEW_FULL_CHAR_VIEW,args);
         data = null;
         first = false;
         args = null;
         System.gc();
      }
      
      protected function getClassName(value:*) : *
      {
         var className:String = getQualifiedClassName(value);
         if(className.lastIndexOf("::") == -1)
         {
            return className;
         }
         return className.slice(className.lastIndexOf("::") + 2);
      }
      
      protected function faceRandomize(first:Boolean = false, silence:Boolean = false, force:Boolean = false) : *
      {
         var buttons:Array = null;
         var key:String = null;
         var selected_button:ClothButton = null;
         var num:Number = NaN;
         var arr:Array = null;
         var selected_color:uint = 0;
         var rnd_mode:String = null;
         var j:uint = 0;
         var age:int = 0;
         var data:Array = new Array();
         var args:Array = new Array();
         for(key in this.faceDict["data"])
         {
            if(!(key == "TatooStyle" || key == "TatooColor"))
            {
               switch(this.getClassName(this.faceDict["data"][key]))
               {
                  case "Array":
                     for(j in this.faceDict["data"][key])
                     {
                        this.faceDict["data"][key][j].selected = false;
                     }
                     selected_button = this.getRandomElementWithWeight(this.faceDict["data"][key]);
                     selected_button.selected = true;
                     data.push({
                        "value":selected_button.index,
                        "choiceGroup":key
                     });
                     break;
                  case "HUISlider":
                     num = 0;
                     if(key == "Age")
                     {
                        num = Math.ceil(Math.pow(Math.random(),4) * 100);
                     }
                     else if(key == "MustacheLength" || key == "BeardLength")
                     {
                        num = 50 + Math.ceil(Math.pow(Math.random(),4) * 50);
                     }
                     else
                     {
                        num = Math.ceil(Math.random() * 100);
                     }
                     this.faceDict["data"][key].valueSilence(num);
                     data.push({
                        "value":num,
                        "choiceGroup":key
                     });
                     break;
                  case "ColorList":
                     arr = new Array(14803168,12695982,10194557,7957854);
                     selected_color = 0;
                     rnd_mode = "normal";
                     if(key == "HairColor")
                     {
                        age = int(this.faceDict["data"]["Age"].value);
                        if(age > 66)
                        {
                           rnd_mode = "over";
                        }
                        else if(age < 33)
                        {
                           rnd_mode = "below";
                        }
                     }
                     selected_color = uint((this.faceDict["data"][key] as ColorList).getRandomColor(arr,rnd_mode));
                     (this.faceDict["data"][key] as ColorList).setColor(selected_color);
                     data.push({
                        "value":selected_color,
                        "choiceGroup":key
                     });
               }
            }
         }
         if(first)
         {
            this._savedHead = data;
         }
         args.push(data);
         args.push(first);
         args.push(force);
         if(silence)
         {
            Api.call(Api.RECEIVEE_RANDOM_PERSONALITY,args);
         }
         else
         {
            Api.call(Api.FULL_FACE_CHAR_VIEW,args);
         }
         this._change_flag = true;
         data = null;
         first = false;
         args = null;
         System.gc();
      }
      
      protected function onDonateUpdateOkButton(event:Event) : *
      {
         Api.call(Api.DONAT_UPDATE_CHAR,[]);
         Api.self.addEventListener(Api.DONAT_UPDATE_CHAR,this.onDonateUpdateCharHandler);
      }
      
      protected function onCreateButtonHandler(event:Event) : void
      {
         this.setFirstChar(false);
         if(this._donat_only_face)
         {
            this.onDonateUpdateOkButton();
            return;
         }
         if(this.isOldChar)
         {
            Api.call(Api.UPDATE_CHAR,[]);
            Api.self.addEventListener(Api.UPDATE_CHAR,this.onUpdateCharHandler);
         }
         else
         {
            Api.self.addEventListener(Api.CREATE_CHAR,this.onCreateCharHandler);
            Api.call(Api.CREATE_CHAR,[]);
         }
         this.setOldChar(false);
         this.setDonatChangeFace(false);
      }
      
      protected function onDonateUpdateCharHandler(event:ApiEvent) : *
      {
         Api.self.removeEventListener(Api.DONAT_UPDATE_CHAR,this.onDonateUpdateCharHandler);
         if(event.data.answer.success == 1)
         {
            this.setOldChar(false);
            this.setDonatChangeFace(false);
            BreadCrumbs.Remove(this.breadCrumb);
            this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.ROOT_SCREEN));
            setTimeout(Character.Update,1500);
         }
         else
         {
            Base.navigator.showDialog("extendedGUI.Dialogs.Error",event.data.answer.msg,true,[new DialogButtonItem("extendedGUI.Dialogs.Ok",null,1)]);
         }
      }
      
      protected function onUpdateCharHandler(event:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.UPDATE_CHAR,this.onCreateCharHandler);
         BreadCrumbs.Remove(this.breadCrumb);
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.ROOT_SCREEN));
         setTimeout(Character.Update,1500);
      }
      
      protected function onCreateCharHandler(event:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.CREATE_CHAR,this.onCreateCharHandler);
         if(event.data.answer.success == 1)
         {
            BreadCrumbs.Remove(this.breadCrumb);
            this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.ROOT_SCREEN));
            setTimeout(Character.Update,1500);
         }
         else
         {
            Base.navigator.showDialog("extendedGUI.Dialogs.Error",event.data.answer.msg,true,[new DialogButtonItem("extendedGUI.Dialogs.Ok",null,1)]);
         }
      }
      
      protected function onBackButtonHandler(event:Event) : void
      {
         this.goBack();
         this.setOldChar(false);
         this.setDonatChangeFace(false);
         this.setFirstChar(false);
      }
      
      protected function onFaceButtonHandler(event:Event) : void
      {
         Api.call(Api.ON_CHAR_MENU_MODE,["face"]);
         this.showFaceMaker(true);
         setTimeout(this.onSelectFace,100,null,this.faceDict["buttons"][0].otherData);
      }
      
      protected function onClothButtonHandler(event:Event) : void
      {
         Api.call(Api.ON_CHAR_MENU_MODE,["cloth"]);
         this.showClothMaker(true);
         this.onSelectCloth(null,this.itemGroups[0]);
      }
      
      protected function showClothMaker(value:Boolean) : void
      {
         if(value)
         {
            this.current_mode = "cloth";
         }
         this.controlBox.visible = !value;
         this.clothControlBox.visible = value;
         this.clothAdditionalBox.visible = value;
      }
      
      protected function showFaceMaker(value:Boolean) : void
      {
         if(value)
         {
            this.current_mode = "face";
            this.addEventListener("onFaceChanged",this.onRealyFaceChanged);
         }
         else
         {
            this.removeEventListener("onFaceChanged",this.onRealyFaceChanged);
         }
         this.controlBox.visible = !value;
         this.faceControlBox.visible = value;
         this.faceAdditionalBox.visible = value;
         this.resizeWidth(this.faceControlBox);
         this.updateFaceMode();
      }
      
      protected function updateFaceMode() : *
      {
         if(this.faceDict == null)
         {
            return;
         }
         var b:Boolean = Boolean(this.faceDict["buttons"][0].selected) && Boolean(this.faceControlBox.visible);
         if(this._faceMode != b)
         {
            this._faceMode = b;
            Api.call(Api.CHAR_MAKER_FACE_MODE,[this._faceMode]);
         }
      }
      
      protected function onBackToMain(event:Event) : void
      {
         this.current_mode = "main";
         Api.call(Api.ON_CHAR_MENU_MODE,[this.current_mode]);
         this.showFaceMaker(false);
         this.showClothMaker(false);
      }
      
      protected function onSelectFace(event:Event, part_name:String = "") : void
      {
         var i:* = undefined;
         var btn:MenuButton2 = null;
         if(part_name == "")
         {
            part_name = (event.target as MenuButton2).otherData;
         }
         for(i in this.faceDict["buttons"])
         {
            btn = this.faceDict["buttons"][i];
            if(part_name != "" && part_name == btn.otherData)
            {
               btn.selected = true;
               if(btn.otherData == "face")
               {
                  is_face = true;
               }
            }
            else
            {
               btn.selected = false;
            }
         }
         this.updateFaceMode();
         this.clearBox(this.faceAdditionalBox);
         this.faceAdditionalBox.addChild(this.faceDict[part_name]["box"]);
         this.resizeFaceAdditional(this.faceAdditionalBox);
      }
      
      protected function onSelectCloth(event:Event, part_name:String = "") : void
      {
         var i:* = undefined;
         var btn:MenuButton2 = null;
         if(part_name == "")
         {
            part_name = (event.target as MenuButton2).otherData;
         }
         for(i in this.itemDict["buttons"])
         {
            btn = this.itemDict["buttons"][i];
            if(part_name != "" && part_name == btn.otherData)
            {
               btn.selected = true;
            }
            else
            {
               btn.selected = false;
            }
         }
         this.clearBox(this.clothAdditionalBox);
         Api.call(Api.ON_SELECT_CLOTH,[part_name]);
         this.clothAdditionalBox.addChild(this.itemDict[part_name]["box"]);
         this.resizeWidth(this.clothAdditionalBox);
      }
      
      override public function goBack() : void
      {
         if(this.current_mode != "main")
         {
            this.onBackToMain(null);
            BreadCrumbs.Remove(this.breadCrumb);
            return;
         }
         Character.CancelCharCreating();
         if(this.isFirstChar)
         {
            Auth.self.doLogout();
         }
         else
         {
            this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.ROOT_SCREEN));
         }
         this.setOldChar(false);
         this.setDonatChangeFace(false);
         this.setFirstChar(false);
         BreadCrumbs.Remove(this.breadCrumb);
      }
      
      private function cancelCharCreating() : void
      {
         Character.CancelCharCreating();
      }
      
      private function clearBox(box:DisplayObjectContainer) : void
      {
         while(box.numChildren > 0)
         {
            box.removeChildAt(0);
         }
      }
      
      private function generateFaceControlsGUI(data:Object) : void
      {
         var part:String = null;
         var i:int = 0;
         var face_quad:* = undefined;
         var face_randomButton:* = undefined;
         var face_backButton:MenuButton2 = null;
         var part_name:String = null;
         var is_empty:Boolean = false;
         var j:int = 0;
         var partBox:VBox = null;
         var part_title:Label = null;
         var faceBox:VBox = null;
         var captionBox:VBox = null;
         var caption:Label = null;
         var otherBox:VBox = null;
         var k:int = 0;
         var obj:Object = null;
         var decr:Label = null;
         var _quad:Quad = null;
         var decrTitle:Label = null;
         var faceButton:MenuButton2 = null;
         Logger.LogToChannel(Logger.DEBUG,"NewCharScreen.onCongigFaceReceived",data);
         this.clearBox(this.faceAdditionalBox);
         this.clearBox(this.faceControlBox);
         this.faceDict = new Object();
         this.faceDict["data"] = new Object();
         this.faceDict["buttons"] = new Array();
         this.faceGroups = new Array();
         var parts:Array = new Array();
         for(part in data)
         {
            parts.push(part);
         }
         parts.sort();
         for(i in parts)
         {
            part_name = parts[i];
            if(part_name.charAt(0) != "#")
            {
               if(this.getClassName(data[part_name]) == "Array")
               {
                  if(!(!this._donat_only_face && Boolean(data[part_name].donat_only)))
                  {
                     is_empty = true;
                     for(j in data[part_name])
                     {
                        if(!(Boolean(data[part_name][j].label) && data[part_name][j].label.charAt(0) == "#"))
                        {
                           if(!(!this._donat_only_face && Boolean(data[part_name][j].donat_only)))
                           {
                              is_empty = false;
                           }
                        }
                     }
                     if(!is_empty)
                     {
                        partBox = new VBox(this.faceControlBox);
                        partBox.alignment = VBox.LEFT;
                        partBox.width = Base.stage.stageWidth * this.SCREEN_PERCENT;
                        partBox.spacing = 1;
                        partBox.debug = false;
                        partBox.backgroundAlpha = 0.5;
                        partBox.backgroundColor = 0;
                        partBox.fixedWidth = Base.stage.stageWidth * this.SCREEN_PERCENT;
                        part_title = new Label(partBox);
                        part_title.autoSize = false;
                        part_title.text = Locale.getById("extendedGUI.NewCharWindow." + part_name.split("_")[1]);
                        part_title.size = 20;
                        part_title.paddingLeft = 13;
                        part_title.color = 8947848;
                        part_title.width = Base.stage.stageWidth * this.SCREEN_PERCENT - part_title.paddingLeft;
                        part_title.height = 35;
                        for(j in data[part_name])
                        {
                           if(!(!this._donat_only_face && Boolean(data[part_name][j].donat_only)))
                           {
                              if(data[part_name][j])
                              {
                                 this.createMenuButton2(partBox,data[part_name][j]);
                              }
                              if(!(Boolean(data[part_name][j].label) && data[part_name][j].label.charAt(0) == "#"))
                              {
                                 this.faceGroups.push(data[part_name][j].label);
                                 this.faceDict[data[part_name][j].label] = new Object();
                                 faceBox = new VBox();
                                 faceBox.alignment = VBox.LEFT;
                                 faceBox.width = Base.stage.stageWidth * this.SCREEN_PERCENT;
                                 faceBox.spacing = 1;
                                 faceBox.debug = false;
                                 this.faceDict[data[part_name][j].label]["box"] = faceBox;
                                 captionBox = new VBox(faceBox);
                                 captionBox.width = Base.stage.stageWidth * this.SCREEN_PERCENT;
                                 captionBox.alignment = VBox.LEFT;
                                 captionBox.spacing = 1;
                                 captionBox.debug = false;
                                 captionBox.backgroundAlpha = 0.9;
                                 captionBox.backgroundColor = 0;
                                 caption = new Label(captionBox);
                                 caption.autoSize = false;
                                 caption.multiline = true;
                                 caption.text = Locale.getById("extendedGUI.NewCharWindow." + data[part_name][j].label) + ":";
                                 caption.size = 22;
                                 caption.paddingTop = 5;
                                 caption.paddingLeft = 13;
                                 caption.paddingBottom = 5;
                                 caption.width = Base.stage.stageWidth * this.SCREEN_PERCENT - caption.paddingLeft;
                                 caption.height = 35;
                                 caption.multiline = true;
                                 this.faceDict[data[part_name][j].label]["caption"] = caption;
                                 otherBox = new VBox(faceBox);
                                 otherBox.alignment = VBox.LEFT;
                                 otherBox.width = Base.stage.stageWidth * this.SCREEN_PERCENT;
                                 otherBox.spacing = 1;
                                 otherBox.debug = false;
                                 otherBox.paddingRight = 13;
                                 otherBox.backgroundAlpha = 0.6;
                                 otherBox.backgroundColor = 0;
                                 for(k in data[part_name][j].data)
                                 {
                                    obj = data[part_name][j].data[k];
                                    if(!(!this._donat_only_face && Boolean(obj.donat_only)))
                                    {
                                       this.createControlItem(otherBox,obj);
                                    }
                                 }
                                 if(data[part_name][j].description)
                                 {
                                    if(Boolean(data[part_name].data) && data[part_name].data.length > 0)
                                    {
                                       _quad = new Quad(otherBox);
                                       _quad.height = 20;
                                       _quad.paddingLeft = 13;
                                       _quad.width = Base.stage.stageWidth * this.SCREEN_PERCENT - _quad.paddingLeft * 2;
                                       decrTitle = new Label(otherBox);
                                       decrTitle.autoSize = false;
                                       decrTitle.text = Locale.getById("extendedGUI.NewCharWindow.destr") + ":";
                                       decrTitle.size = 22;
                                       decrTitle.paddingLeft = 13;
                                       decrTitle.width = Base.stage.stageWidth * this.SCREEN_PERCENT - caption.paddingLeft;
                                       decrTitle.height = 35;
                                       decrTitle.multiline = true;
                                    }
                                    decr = new Label(otherBox);
                                    decr.autoSize = false;
                                    decr.text = Locale.getById("extendedGUI.NewCharWindow." + data[part_name][j].description);
                                    decr.size = 18;
                                    if(!(data[part_name].data && data[part_name].data.length > 0))
                                    {
                                       decr.paddingTop = 10;
                                    }
                                    decr.paddingLeft = 13;
                                    decr.paddingBottom = 13;
                                    decr.width = Base.stage.stageWidth * this.SCREEN_PERCENT - decr.paddingLeft;
                                    decr.height = 35;
                                    decr.word;
                                    decr.multiline = true;
                                 }
                              }
                           }
                        }
                     }
                  }
               }
               else if(this.getClassName(data[part_name]) == "Object")
               {
                  if(!(!this._donat_only_face && Boolean(data[part_name].donat_only)))
                  {
                     this.faceGroups.push(data[part_name].label);
                     this.faceDict[data[part_name].label] = new Object();
                     faceButton = new MenuButton2(this.faceControlBox,0,0,"",this.onSelectFace);
                     faceButton.$ = "extendedGUI.NewCharWindow." + data[part_name].label;
                     faceButton.height = 35;
                     faceButton.autoWidthPercentScreen = this.SCREEN_PERCENT;
                     faceButton.toggle = true;
                     faceButton.selected = false;
                     faceButton.marginLeft = 13;
                     faceButton.otherData = data[part_name].label;
                     this.faceDict["buttons"].push(faceButton);
                     faceBox = new VBox();
                     faceBox.alignment = VBox.LEFT;
                     faceBox.width = Base.stage.stageWidth * this.SCREEN_PERCENT;
                     faceBox.spacing = 1;
                     faceBox.debug = false;
                     this.faceDict[data[part_name].label]["box"] = faceBox;
                     captionBox = new VBox(faceBox);
                     captionBox.width = Base.stage.stageWidth * this.SCREEN_PERCENT;
                     captionBox.alignment = VBox.LEFT;
                     captionBox.spacing = 1;
                     captionBox.debug = false;
                     captionBox.backgroundAlpha = 0.9;
                     captionBox.backgroundColor = 0;
                     caption = new Label(captionBox);
                     caption.autoSize = false;
                     caption.multiline = true;
                     caption.text = Locale.getById("extendedGUI.NewCharWindow." + data[part_name].label) + ":";
                     caption.size = 22;
                     caption.paddingTop = 5;
                     caption.paddingLeft = 13;
                     caption.paddingBottom = 5;
                     caption.width = Base.stage.stageWidth * this.SCREEN_PERCENT - caption.paddingLeft;
                     caption.height = 35;
                     caption.multiline = true;
                     this.faceDict[data[part_name].label]["caption"] = caption;
                     otherBox = new VBox(faceBox);
                     otherBox.alignment = VBox.LEFT;
                     otherBox.width = Base.stage.stageWidth * this.SCREEN_PERCENT;
                     otherBox.spacing = 1;
                     otherBox.debug = false;
                     otherBox.paddingRight = 13;
                     otherBox.backgroundAlpha = 0.6;
                     otherBox.backgroundColor = 0;
                     for(k in data[part_name].data)
                     {
                        obj = data[part_name].data[k];
                        this.createControlItem(otherBox,obj);
                     }
                     if(data[part_name].description)
                     {
                        if(Boolean(data[part_name].data) && data[part_name].data.length > 0)
                        {
                           _quad = new Quad(otherBox);
                           _quad.height = 20;
                           _quad.paddingLeft = 13;
                           _quad.width = Base.stage.stageWidth * this.SCREEN_PERCENT - _quad.paddingLeft * 2;
                           decrTitle = new Label(otherBox);
                           decrTitle.autoSize = false;
                           decrTitle.text = Locale.getById("extendedGUI.NewCharWindow.destr") + ":";
                           decrTitle.size = 22;
                           decrTitle.paddingLeft = 13;
                           decrTitle.width = Base.stage.stageWidth * this.SCREEN_PERCENT - caption.paddingLeft;
                           decrTitle.height = 35;
                           decrTitle.multiline = true;
                        }
                        decr = new Label(otherBox);
                        decr.autoSize = false;
                        decr.text = Locale.getById("extendedGUI.NewCharWindow." + data[part_name].description);
                        decr.size = 18;
                        if(!(data[part_name].data && data[part_name].data.length > 0))
                        {
                           decr.paddingTop = 10;
                        }
                        decr.paddingLeft = 13;
                        decr.paddingBottom = 13;
                        decr.width = Base.stage.stageWidth * this.SCREEN_PERCENT - caption.paddingLeft;
                        decr.height = 35;
                        decr.word;
                        decr.multiline = true;
                     }
                  }
               }
            }
         }
         face_quad = new Quad(this.faceControlBox);
         face_quad.width = Base.stage.stageWidth * this.SCREEN_PERCENT;
         face_quad.height = 20;
         this.faceResetButton = new MenuButton2(this.faceControlBox);
         this.faceResetButton.$ = "extendedGUI.NewCharWindow.reset";
         this.faceResetButton.autoWidthPercentScreen = this.SCREEN_PERCENT;
         this.faceResetButton.height = 35;
         this.faceResetButton.enabled = false;
         this.faceResetButton.marginLeft = 13;
         this.faceResetButton.addEventListener(MouseEvent.CLICK,this.faceresetClickHandler);
         face_randomButton = new MenuButton2(this.faceControlBox);
         face_randomButton.$ = "extendedGUI.NewCharWindow.randomizeChar";
         face_randomButton.autoWidthPercentScreen = this.SCREEN_PERCENT;
         face_randomButton.height = 35;
         face_randomButton.marginLeft = 13;
         face_randomButton.addEventListener(MouseEvent.CLICK,this.facerandomClickHandler);
         face_backButton = new MenuButton2(this.faceControlBox,0,0,"",this.onBackToMain);
         face_backButton.$ = "extendedGUI.NewCharWindow.applyButton";
         face_backButton.autoWidthPercentScreen = this.SCREEN_PERCENT;
         face_backButton.height = 35;
         face_backButton.marginLeft = 13;
         this.faceBackButton = new MenuButton2(this.faceControlBox);
         this.faceBackButton.$ = "extendedGUI.RootWindow.backButton";
         this.faceBackButton.autoWidthPercentScreen = this.SCREEN_PERCENT;
         this.faceBackButton.height = 35;
         this.faceBackButton.marginLeft = 13;
         this.faceBackButton.addEventListener(MouseEvent.CLICK,this.faceresetBackHandler);
         this.faceSaveButton = new MenuButton2(this.faceControlBox);
         this.faceSaveButton.$ = "extendedGUI.NewCharWindow.saveFace";
         this.faceSaveButton.autoWidthPercentScreen = this.SCREEN_PERCENT;
         this.faceSaveButton.height = 35;
         this.faceSaveButton.marginLeft = 13;
         this.faceSaveButton.addEventListener(MouseEvent.CLICK,this.faceSaveClickHandler);
         this.faceLoadButton = new MenuButton2(this.faceControlBox);
         this.faceLoadButton.$ = "extendedGUI.NewCharWindow.loadFace";
         this.faceLoadButton.autoWidthPercentScreen = this.SCREEN_PERCENT;
         this.faceLoadButton.height = 35;
         this.faceLoadButton.marginLeft = 13;
         this.faceLoadButton.addEventListener(MouseEvent.CLICK,this.faceLoadClickHandler);
         this.resizeWidth(this.faceControlBox);
         this.faceAdditionalBox.draw();
         this.faceControlBox.draw();
         this.faceControlBox.visible = false;
         this.faceAdditionalBox.visible = false;
      }
      
      private function onCongigFaceReceived(data:Object) : void
      {
         this.generateFaceControlsGUI(data);
         this.faceRandomize(true);
         this.onSelectFace(null,this.faceDict["buttons"][0].otherData);
         this.checkAccessLevelForButtons();
      }
      
      private function onConfigDonateChangeFaceRecived(data:*) : void
      {
         this.generateFaceControlsGUI(data);
         this.onSelectFace(null,this.faceDict["buttons"][0].otherData);
         this.checkAccessLevelForButtons();
         this._create_flag_python = true;
         this.enabledCharButtons(true);
      }
      
      private function onChangeFaceCostRecived(data:Object) : void
      {
         this.CHANGE_FACE_GOLD_STRING = data.value;
      }
      
      private function onConfigReceived(data:Object) : void
      {
         var part:String = null;
         var i:int = 0;
         var _quad1:Quad = null;
         var clothRandomButton:MenuButton2 = null;
         var cloth_backButton:MenuButton2 = null;
         var part_name:String = null;
         var clothButton:MenuButton2 = null;
         var clothBox:VBox = null;
         var captionBox:VBox = null;
         var caption:Label = null;
         var otherBox:VBox = null;
         var itemBox:HBox = null;
         var index:int = 0;
         var _quad:Quad = null;
         var item_name:Label = null;
         var descr:Label = null;
         var _quad2:Quad = null;
         var color_label:Label = null;
         var colorList:ColorList = null;
         var obj:Object = null;
         var item:ClothButton = null;
         Logger.LogToChannel(Logger.DEBUG,"NewCharScreen.onConfigReceived",data);
         this.clearBox(this.clothAdditionalBox);
         this.clearBox(this.clothControlBox);
         this.itemDict = new Object();
         this.itemDict["buttons"] = new Array();
         this.itemGroups = new Array();
         var parts:Array = new Array();
         for(part in data)
         {
            parts.push(part);
         }
         parts.sort();
         for(i in parts)
         {
            part_name = parts[i];
            this.itemGroups.push(part_name);
            this.itemDict[part_name] = new Object();
            clothButton = new MenuButton2(this.clothControlBox,0,0,"",this.onSelectCloth);
            clothButton.$ = "extendedGUI.NewCharWindow." + part_name.split("_")[1];
            clothButton.height = 35;
            clothButton.autoWidthPercentScreen = this.SCREEN_PERCENT;
            clothButton.toggle = true;
            clothButton.selected = true;
            clothButton.marginLeft = 13;
            clothButton.otherData = part_name;
            this.itemDict["buttons"].push(clothButton);
            clothBox = new VBox();
            clothBox.alignment = VBox.LEFT;
            clothBox.width = Base.stage.stageWidth * this.SCREEN_PERCENT;
            clothBox.spacing = 1;
            clothBox.debug = false;
            captionBox = new VBox(clothBox);
            captionBox.width = Base.stage.stageWidth * this.SCREEN_PERCENT;
            captionBox.alignment = VBox.LEFT;
            captionBox.spacing = 1;
            captionBox.debug = false;
            captionBox.backgroundAlpha = 0.9;
            captionBox.backgroundColor = 0;
            caption = new Label(captionBox);
            caption.autoSize = false;
            caption.text = Locale.getById("extendedGUI.NewCharWindow." + part_name.split("_")[1] + "_caption");
            caption.size = 22;
            caption.paddingTop = 5;
            caption.paddingLeft = 13;
            caption.paddingBottom = 5;
            caption.width = Base.stage.stageWidth * this.SCREEN_PERCENT - caption.paddingLeft;
            caption.height = 35;
            caption.multiline = true;
            this.itemDict[part_name]["caption"] = caption;
            otherBox = new VBox(clothBox);
            otherBox.alignment = VBox.LEFT;
            otherBox.width = Base.stage.stageWidth * this.SCREEN_PERCENT;
            otherBox.spacing = 1;
            otherBox.debug = false;
            otherBox.paddingRight = 13;
            otherBox.backgroundAlpha = 0.6;
            otherBox.backgroundColor = 0;
            itemBox = new HBox();
            itemBox.alignment = HBox.MIDDLE;
            itemBox.fixedHeight = 200;
            itemBox.fixedWidth = Base.stage.stageWidth * this.SCREEN_PERCENT;
            itemBox.horizontalAlign = HBox.LEFT;
            itemBox.spacing = 10;
            itemBox.debug = false;
            this.itemDict[part_name]["buttons"] = new Array();
            for(index in data[part_name])
            {
               obj = data[part_name][index];
               item = new ClothButton(itemBox);
               item.width = (itemBox.fixedWidth - itemBox.spacing * data[part_name].length) / data[part_name].length;
               item.texturePath = obj.texture;
               item.description = obj.description;
               item.item_name = obj.caption;
               item.colors = obj.colors;
               item.item_id = obj.item_id;
               item.group = part_name;
               item.addEventListener(MouseEvent.CLICK,this.onClothClick);
               this.itemDict[part_name]["buttons"].push(item);
            }
            otherBox.addChild(itemBox);
            _quad = new Quad(otherBox);
            _quad.height = 20;
            _quad.paddingLeft = 13;
            _quad.width = Base.stage.stageWidth * this.SCREEN_PERCENT - _quad.paddingLeft * 2;
            item_name = new Label(otherBox);
            item_name.autoSize = false;
            item_name.text = part_name;
            item_name.multiline = true;
            item_name.size = 22;
            item_name.paddingLeft = 13;
            this.itemDict[part_name]["item_name"] = item_name;
            this.itemDict[part_name]["box"] = clothBox;
            descr = new Label(otherBox);
            descr.autoSize = false;
            descr.multiline = true;
            descr.text = part_name;
            descr.size = 20;
            descr.paddingLeft = 13;
            descr.width = Base.stage.stageWidth * this.SCREEN_PERCENT - descr.paddingLeft;
            this.itemDict[part_name]["descr"] = descr;
            _quad2 = new Quad(otherBox);
            _quad2.height = 20;
            _quad2.paddingLeft = 13;
            _quad2.width = Base.stage.stageWidth * this.SCREEN_PERCENT - _quad2.paddingLeft * 2;
            color_label = new Label(otherBox);
            color_label.autoSize = false;
            color_label.$ = "extendedGUI.NewCharWindow.color";
            color_label.paddingLeft = 13;
            color_label.width = Base.stage.stageWidth * this.SCREEN_PERCENT - color_label.paddingLeft * 2;
            color_label.size = 20;
            colorList = new ColorList(otherBox);
            colorList.height = 100;
            colorList.otherData = part_name;
            colorList.paddingTop = 10;
            colorList.paddingLeft = 13;
            colorList.paddingBottom = 10;
            colorList.width = Base.stage.stageWidth * this.SCREEN_PERCENT - colorList.paddingLeft;
            colorList.addEventListener(ColorEvent.SELECT,this.onColorSelect);
            this.itemDict[part_name]["colors"] = colorList;
            Logger.LogToChannel(Logger.DEBUG,"part_name:",part_name);
         }
         _quad1 = new Quad(this.clothControlBox);
         _quad1.width = Base.stage.stageWidth * this.SCREEN_PERCENT;
         _quad1.height = 20;
         clothRandomButton = new MenuButton2(this.clothControlBox,0,0,"",this.clothRandomClickHandler);
         clothRandomButton.$ = "extendedGUI.NewCharWindow.randomizeChar";
         clothRandomButton.autoWidthPercentScreen = this.SCREEN_PERCENT;
         clothRandomButton.height = 35;
         clothRandomButton.marginLeft = 13;
         this.clothResetButton = new MenuButton2(this.clothControlBox,0,0,"",this.clothresetClickHandler);
         this.clothResetButton.$ = "extendedGUI.NewCharWindow.reset";
         this.clothResetButton.autoWidthPercentScreen = this.SCREEN_PERCENT;
         this.clothResetButton.height = 35;
         this.clothResetButton.enabled = false;
         this.clothResetButton.marginLeft = 13;
         cloth_backButton = new MenuButton2(this.clothControlBox,0,0,"",this.onBackToMain);
         cloth_backButton.$ = "extendedGUI.NewCharWindow.backButton";
         cloth_backButton.autoWidthPercentScreen = this.SCREEN_PERCENT;
         cloth_backButton.height = 35;
         cloth_backButton.marginLeft = 13;
         this.charNameBox.draw();
         this.clothAdditionalBox.draw();
         this.controlBox.draw();
         this.clothControlBox.visible = false;
         this.clothAdditionalBox.visible = false;
         this.randomize(true);
         this.onSelectCloth(null,this.itemGroups[0]);
         setTimeout(this.setFocus,0);
         setTimeout(Dummy.show,30);
         Api.call(Api.ON_CHAR_MENU_MODE,["main"]);
      }
      
      protected function onColorSelect(event:ColorEvent) : *
      {
         var i:int = 0;
         var group_name:String = event.targetList.otherData;
         var buttons:Array = this.itemDict[group_name]["buttons"];
         var item_id:Number = 0;
         var obj:Object = new Object();
         for(i in buttons)
         {
            if(buttons[i].selected == true)
            {
               item_id = Number(buttons[i].item_id);
               buttons[i].selectedColor = event.color;
               break;
            }
         }
         obj["item_id"] = item_id;
         obj["color"] = event.color;
         this.cloth_call([{
            "var":obj,
            "choiceGroup":group_name
         }]);
         this._change_flag = true;
      }
      
      protected function onClothClick(event:MouseEvent) : *
      {
         var i:int = 0;
         var item_name:* = null;
         var col_tmp:int = 0;
         var obj:Object = new Object();
         var buttons:Array = this.itemDict[(event.target as ClothButton).group]["buttons"];
         var old_item_id:int = 0;
         for(i in buttons)
         {
            if(buttons[i].selected)
            {
               old_item_id = int(buttons[i].item_id);
            }
            buttons[i].selected = false;
         }
         (event.target as ClothButton).selected = true;
         item_name = (event.target as ClothButton).item_name;
         if((event.target as ClothButton).description != "")
         {
            item_name = Locale.getById("extendedGUI.NewCharWindow.destr") + " " + item_name + ":";
         }
         this.itemDict[(event.target as ClothButton).group]["item_name"].text = item_name;
         this.itemDict[(event.target as ClothButton).group]["descr"].text = (event.target as ClothButton).description;
         this.itemDict[(event.target as ClothButton).group]["colors"].setData((event.target as ClothButton).colors);
         if((event.target as ClothButton).selectedColor == 0)
         {
            (event.target as ClothButton).selectedColor = int("0x" + (event.target as ClothButton).colors[0].split(":")[0]).toString(10);
         }
         this.itemDict[(event.target as ClothButton).group]["colors"].setColor((event.target as ClothButton).selectedColor);
         obj["item_id"] = (event.target as ClothButton).item_id;
         obj["old_id"] = old_item_id;
         obj["color"] = this.itemDict[(event.target as ClothButton).group]["colors"].getColor();
         this.cloth_call([{
            "var":obj,
            "choiceGroup":(event.target as ClothButton).group
         }]);
      }
      
      override protected function resize(... args) : void
      {
         if(this.charNameInput == null)
         {
            return;
         }
         this.charNameInput.setSize(Base.stage.stageWidth * this.SCREEN_PERCENT,40);
         this.quad.width = Base.stage.stageWidth * this.SCREEN_PERCENT;
         this.clothAdditionalBox.x = Base.stage.stageWidth - Base.stage.stageWidth * this.SCREEN_PERCENT - 30;
         this.faceAdditionalBox.x = this.clothAdditionalBox.x;
         this.resizeWidth(this.controlBox);
         this.resizeWidth(this.clothAdditionalBox);
         this.resizeWidth(this.clothControlBox);
         this.resizeWidth(this.faceControlBox);
         this.resizeFaceAdditional(this.faceAdditionalBox);
      }
      
      protected function resizeFaceAdditional(parent:*) : *
      {
         var class_name:String = null;
         var _color:uint = 0;
         var colums:int = 0;
         for(var i:int = 0; i < parent.numChildren; i++)
         {
            class_name = this.getClassName(parent.getChildAt(i));
            if(class_name == "VBox" || class_name == "HBox")
            {
               (parent.getChildAt(i) as Component).width = Base.stage.stageWidth * this.SCREEN_PERCENT;
               if(class_name == "HBox")
               {
                  (parent.getChildAt(i) as HBox).fixedWidth = Base.stage.stageWidth * this.SCREEN_PERCENT;
               }
               else
               {
                  (parent.getChildAt(i) as VBox).fixedWidth = Base.stage.stageWidth * this.SCREEN_PERCENT;
               }
               this.resizeFaceAdditional(parent.getChildAt(i));
               parent.getChildAt(i).draw();
            }
            else if(class_name == "Label")
            {
               if(!(parent.getChildAt(i) as Label).dontResize)
               {
                  if(this.getClassName(parent) == "HBox")
                  {
                     (parent.getChildAt(i) as Label).width = (parent as HBox).fixedWidth - 13;
                  }
                  else
                  {
                     (parent.getChildAt(i) as Label).width = parent.width;
                  }
               }
            }
            else if(class_name == "ColorList")
            {
               _color = uint((parent.getChildAt(i) as ColorList).getColor());
               (parent.getChildAt(i) as ColorList).width = Base.stage.stageWidth * this.SCREEN_PERCENT - 13;
               (parent.getChildAt(i) as ColorList).setData((parent.getChildAt(i) as ColorList).allColors);
               (parent.getChildAt(i) as ColorList).setColor(_color);
            }
            else if(class_name == "ClothButton")
            {
               colums = int((parent.getChildAt(i) as ClothButton).additionalData);
               (parent.getChildAt(i) as ClothButton).width = (parent as HBox).fixedWidth / colums - 13 * 2;
            }
            else if(class_name == "HUISlider")
            {
               (parent.getChildAt(i) as HUISlider).width = Base.stage.stageWidth * this.SCREEN_PERCENT * 2 / 3 - (parent.getChildAt(i) as HUISlider).paddingLeft * 2;
            }
            else if(class_name == "Quad")
            {
               (parent.getChildAt(i) as Quad).width = (parent as VBox).width - (parent.getChildAt(i) as Quad).paddingLeft * 2;
            }
         }
      }
      
      protected function resizeWidth(parent:*, tt:String = "") : *
      {
         var class_name:String = null;
         var _color:uint = 0;
         for(var i:int = 0; i < parent.numChildren; i++)
         {
            class_name = this.getClassName(parent.getChildAt(i));
            tt += " ";
            if(class_name == "VBox" || class_name == "HBox")
            {
               (parent.getChildAt(i) as Component).width = Base.stage.stageWidth * this.SCREEN_PERCENT;
               if(class_name == "HBox")
               {
                  (parent.getChildAt(i) as HBox).fixedWidth = Base.stage.stageWidth * this.SCREEN_PERCENT;
               }
               else
               {
                  (parent.getChildAt(i) as VBox).fixedWidth = Base.stage.stageWidth * this.SCREEN_PERCENT;
               }
               this.resizeWidth(parent.getChildAt(i));
            }
            else if(class_name == "Label")
            {
               if(this.getClassName(parent) == "HBox")
               {
                  (parent.getChildAt(i) as Label).width = Base.stage.stageWidth * this.SCREEN_PERCENT * 1 / 3 - (parent.getChildAt(i) as Label).paddingLeft;
               }
               else
               {
                  (parent.getChildAt(i) as Label).width = Base.stage.stageWidth * this.SCREEN_PERCENT - (parent.getChildAt(i) as Label).paddingLeft;
               }
            }
            else if(class_name == "ClothButton")
            {
               (parent.getChildAt(i) as ClothButton).width = ((parent as HBox).width - parent.numChildren * (parent as HBox).spacing) / parent.numChildren;
            }
            else if(class_name == "PictureButton")
            {
               (parent.getChildAt(i) as PictureButton).width = ((parent as HBox).width - parent.numChildren * (parent as HBox).spacing) / parent.numChildren;
            }
            else if(class_name == "Quad")
            {
               (parent.getChildAt(i) as Quad).width = (parent as VBox).width - (parent.getChildAt(i) as Quad).paddingLeft * 2;
            }
            else if(class_name == "ColorList")
            {
               _color = uint((parent.getChildAt(i) as ColorList).getColor());
               (parent.getChildAt(i) as ColorList).width = (parent as VBox).width - (parent.getChildAt(i) as ColorList).paddingLeft * 2;
               (parent.getChildAt(i) as ColorList).setData((parent.getChildAt(i) as ColorList).allColors);
               (parent.getChildAt(i) as ColorList).setColor(_color);
            }
            else if(class_name == "MenuButton2")
            {
               (parent.getChildAt(i) as MenuButton2).draw();
            }
            else if(class_name == "InputText")
            {
               (parent.getChildAt(i) as InputText).setSize(Base.stage.stageWidth * this.SCREEN_PERCENT * 2 / 3 - 13,40);
            }
            else if(class_name == "Slider")
            {
               (parent.getChildAt(i) as Slider).width = parent.width * 2 / 3;
            }
         }
      }
      
      protected function createColorList(parent:DisplayObjectContainer, data:Object) : ColorList
      {
         var tmp_label:Label = null;
         if(Boolean(data.label) && data.label != "")
         {
            tmp_label = new Label(parent);
            tmp_label.autoSize = false;
            tmp_label.multiline = true;
            tmp_label.$ = "extendedGUI.NewCharWindow." + data.label;
            tmp_label.paddingLeft = 13;
            tmp_label.width = Base.stage.stageWidth * this.SCREEN_PERCENT / 3 - tmp_label.paddingLeft;
            tmp_label.size = 20;
            tmp_label.paddingTop = 10;
         }
         var colorList:ColorList = new ColorList(parent);
         colorList.paddingLeft = 13;
         colorList.paddingTop = 10;
         colorList.paddingBottom = 13;
         colorList.paddingRight = 13;
         colorList.width = Base.stage.stageWidth * this.SCREEN_PERCENT - colorList.paddingLeft;
         colorList.setData(data.colors);
         colorList.otherData = data.id;
         colorList.addEventListener(ColorEvent.SELECT,this["on" + data.id + "Change"]);
         return colorList;
      }
      
      protected function createSlider(parent:DisplayObjectContainer, data:Object) : HUISlider
      {
         var tmp_label:Label = null;
         var box:HBox = new HBox(parent);
         box.fixedWidth = Base.stage.stageWidth * this.SCREEN_PERCENT;
         box.fixedHeight = 35;
         box.spacing = 0;
         box.height = 35;
         var w:Number = Base.stage.stageWidth * this.SCREEN_PERCENT - 13 * 2;
         if(Boolean(data.label) && data.label != "")
         {
            tmp_label = new Label(box);
            tmp_label.autoSize = false;
            tmp_label.$ = "extendedGUI.NewCharWindow." + data.label;
            tmp_label.paddingLeft = 13;
            tmp_label.width = box.fixedWidth / 3 - tmp_label.paddingLeft;
            tmp_label.draw();
            tmp_label.height = 35;
            tmp_label.size = 20;
            tmp_label.paddingTop = 10;
            tmp_label.dontResize = true;
            w = box.fixedWidth * 2 / 3;
         }
         var slider:HUISlider = new HUISlider(box);
         slider.width = w;
         slider.height = 35;
         slider.paddingRight = 0;
         slider.paddingLeft = 0;
         slider.minimum = 0;
         slider.maximum = 100;
         slider.tick = 1;
         slider.labelPrecision = 0;
         slider.valueSilence(0);
         slider.initValue = 0;
         slider.defaultValue = 0;
         slider.addEventListener(Event.CHANGE,this["on" + data.id + "Change"]);
         return slider;
      }
      
      protected function createSliderList(parent:DisplayObjectContainer, data:Object) : Array
      {
         var i:int = 0;
         var slider:HUISlider = null;
         var result:Array = new Array();
         var box:HBox = new HBox(parent);
         box.alignment = HBox.MIDDLE;
         box.fixedheight = 35;
         box.fixedWidth = Base.stage.stageWidth * this.SCREEN_PERCENT;
         box.horizontalAlign = HBox.LEFT;
         box.spacing = 30;
         var list:Array = data.data;
         for(i in list)
         {
            slider = new HUISlider(box);
            slider.width = (box.fixedWidth - list.length * box.spacing) / list.length;
            slider.height = 35;
            slider.paddingRight = 0;
            slider.minimum = 0;
            slider.maximum = 100;
            slider.tick = 1;
            slider.labelPrecision = 0;
            slider.valueSilence(0);
            slider.initValue = 0;
            slider.defaultValue = 0;
            slider.addEventListener(Event.CHANGE,this["on" + list[i].id + "Change"]);
            this.faceDict["data"][list.id] = slider;
            result.push(slider);
         }
         return result;
      }
      
      protected function createPictureButtonList(parent:DisplayObjectContainer, data:Object) : Array
      {
         var i:int = 0;
         var box:HBox = null;
         var texture_path:String = null;
         var button:ClothButton = null;
         var result:Array = new Array();
         var vbox:VBox = new VBox(parent);
         vbox.alignment = VBox.LEFT;
         var list:Array = data.data;
         var colums:Number = 999;
         if(data.colums != null)
         {
            colums = Number(data.colums);
         }
         var rows:int = Math.ceil(list.length / colums);
         var vert_boxes:Array = new Array();
         for(var j:int = 0; j < rows; j++)
         {
            box = new HBox(vbox);
            box.alignment = HBox.MIDDLE;
            box.fixedHeight = 100;
            box.paddingLeft = 13;
            box.paddingTop = 10;
            box.fixedWidth = Base.stage.stageWidth * this.SCREEN_PERCENT;
            box.horizontalAlign = HBox.LEFT;
            box.spacing = 30;
            vert_boxes.push(box);
         }
         var count:int = 0;
         var index:int = 0;
         for(i in list)
         {
            if(!(!this._donat_only_face && Boolean(list[i].donat_only)))
            {
               if(count >= colums)
               {
                  count = 0;
                  index++;
               }
               texture_path = list[i].image;
               button = new ClothButton(vert_boxes[index]);
               button.texturePath = texture_path;
               button.width = (vert_boxes[index].fixedWidth - vert_boxes[index].paddingLeft * 2 - list.length * vert_boxes[index].spacing) / Math.min(colums,list.length - 1);
               button.height = 90;
               button.group = data.id;
               button.index = list[i].item_id;
               button.weight = uint(list[i].weight) || 100;
               button.addEventListener(MouseEvent.CLICK,this["on" + data.id + "Change"]);
               button.additionalData = colums;
               result.push(button);
               count++;
            }
         }
         return result;
      }
      
      protected function onSkinColorChange(event:ColorEvent) : *
      {
         this.face_call([{
            "choiceGroup":"SkinColor",
            "value":event.color
         }]);
         this.onFaceChanged(null,"onSkinColorChange");
      }
      
      protected function onTatooColorChange(event:ColorEvent) : *
      {
         this.face_call([{
            "choiceGroup":"TatooColor",
            "value":event.color
         }]);
         this.onFaceChanged(null,"onTatooColorChange");
      }
      
      protected function onAgeChange(event:Event) : *
      {
         this.face_call([{
            "choiceGroup":"Age",
            "value":(event.target as HUISlider).value
         }]);
         this.onFaceChanged(null,"onAgeChange");
      }
      
      protected function onDetailsChange(event:Event) : *
      {
         this.face_call([{
            "choiceGroup":"Details",
            "value":(event.target as HUISlider).value
         }]);
         this.onFaceChanged(null,"onDetailsChange");
      }
      
      protected function onHairStyleChange(event:MouseEvent) : *
      {
         var i:uint = 0;
         var button:ClothButton = event.target as ClothButton;
         var buttons:Array = this.faceDict["data"][button.group];
         for(i in buttons)
         {
            buttons[i].selected = false;
         }
         button.selected = true;
         this.face_call([{
            "choiceGroup":"HairStyle",
            "value":button.index
         }]);
         this.onFaceChanged(null,"onHairStyleChange");
      }
      
      protected function onHairLengthChange(event:Event) : *
      {
         this.face_call([{
            "choiceGroup":"HairLength",
            "value":(event.target as HUISlider).value
         }]);
         this.onFaceChanged(null,"onHairLengthChange");
      }
      
      protected function onEyebrowsStyleChange(event:MouseEvent) : *
      {
         var i:uint = 0;
         var button:ClothButton = event.target as ClothButton;
         var buttons:Array = this.faceDict["data"][button.group];
         for(i in buttons)
         {
            buttons[i].selected = false;
         }
         button.selected = true;
         this.face_call([{
            "choiceGroup":"EyebrowsStyle",
            "value":button.index
         }]);
         this.onFaceChanged(null,"onEyebrowsStyleChange");
      }
      
      protected function onEyebrowsPositionChange(event:Event) : *
      {
         this.face_call([{
            "choiceGroup":"EyebrowsPosition",
            "value":(event.target as HUISlider).value
         }]);
         this.onFaceChanged(null,"onEyebrowsPositionChange");
      }
      
      protected function onEyebrowsRotationChange(event:Event) : *
      {
         this.face_call([{
            "choiceGroup":"EyebrowsRotation",
            "value":(event.target as HUISlider).value
         }]);
         this.onFaceChanged(null,"onEyebrowsRotationChange");
      }
      
      protected function onTatooPositionXChange(event:Event) : *
      {
         this.face_call([{
            "choiceGroup":"TatooPositionX",
            "value":(event.target as HUISlider).value
         }]);
         this.onFaceChanged(null,"onTatooPositionXChange");
      }
      
      protected function onTatooPositionYChange(event:Event) : *
      {
         this.face_call([{
            "choiceGroup":"TatooPositionY",
            "value":(event.target as HUISlider).value
         }]);
         this.onFaceChanged(null,"onTatooPositionYChange");
      }
      
      protected function onTatooRotationChange(event:Event) : *
      {
         this.face_call([{
            "choiceGroup":"TatooRotation",
            "value":(event.target as HUISlider).value
         }]);
         this.onFaceChanged(null,"onTatooRotationChange");
      }
      
      protected function onTatooSizeChange(event:Event) : *
      {
         this.face_call([{
            "choiceGroup":"TatooSize",
            "value":(event.target as HUISlider).value
         }]);
         this.onFaceChanged(null,"onTatooSizeChange");
      }
      
      protected function onTatooStyleChange(event:MouseEvent) : *
      {
         var i:uint = 0;
         var button:ClothButton = event.target as ClothButton;
         var buttons:Array = this.faceDict["data"][button.group];
         for(i in buttons)
         {
            buttons[i].selected = false;
         }
         button.selected = true;
         this.face_call([{
            "choiceGroup":"TatooStyle",
            "value":button.index
         }]);
         this.onFaceChanged(null,"onTatooStyleChange");
      }
      
      protected function onMustacheStyleChange(event:MouseEvent) : *
      {
         var i:uint = 0;
         var button:ClothButton = event.target as ClothButton;
         var buttons:Array = this.faceDict["data"][button.group];
         for(i in buttons)
         {
            buttons[i].selected = false;
         }
         button.selected = true;
         this.face_call([{
            "choiceGroup":"MustacheStyle",
            "value":button.index
         }]);
         this.onFaceChanged(null,"onMustacheStyleChange");
      }
      
      protected function onMustacheLengthChange(event:Event) : *
      {
         this.face_call([{
            "choiceGroup":"MustacheLength",
            "value":(event.target as HUISlider).value
         }]);
         this.onFaceChanged(null,"onMustacheLengthChange");
      }
      
      protected function onUnshavenChange(event:Event) : *
      {
         this.face_call([{
            "choiceGroup":"Unshaven",
            "value":(event.target as HUISlider).value
         }]);
         this.onFaceChanged(null,"onUnshavenChange");
      }
      
      protected function onBeardStyleChange(event:MouseEvent) : *
      {
         var i:uint = 0;
         var button:ClothButton = event.target as ClothButton;
         var buttons:Array = this.faceDict["data"][button.group];
         for(i in buttons)
         {
            buttons[i].selected = false;
         }
         button.selected = true;
         this.face_call([{
            "choiceGroup":"BeardStyle",
            "value":button.index
         }]);
         this.onFaceChanged(null,"onBeardStyleChange");
      }
      
      protected function onBeardLengthChange(event:Event) : *
      {
         this.face_call([{
            "choiceGroup":"BeardLength",
            "value":(event.target as HUISlider).value
         }]);
         this.onFaceChanged(null,"onBeardLengthChange");
      }
      
      protected function onHairColorChange(event:ColorEvent) : *
      {
         this.face_call([{
            "choiceGroup":"HairColor",
            "value":event.color
         }]);
         this.onFaceChanged(null,"onHairColorChange");
      }
      
      protected function onEyeColorChange(event:ColorEvent) : *
      {
         this.face_call([{
            "choiceGroup":"EyeColor",
            "value":event.color
         }]);
         this.onFaceChanged(null,"onEyeColorChange");
      }
      
      protected function createMenuButton2(parent:DisplayObjectContainer, item:Object) : MenuButton2
      {
         var faceButton:MenuButton2 = new MenuButton2(parent,0,0,"",this.onSelectFace);
         faceButton.$ = "extendedGUI.NewCharWindow." + item.label;
         faceButton.height = 35;
         faceButton.autoWidthPercentScreen = this.SCREEN_PERCENT;
         faceButton.toggle = true;
         faceButton.selected = true;
         faceButton.marginLeft = 26;
         faceButton.otherData = item.label;
         this.faceDict["buttons"].push(faceButton);
         return faceButton;
      }
      
      protected function onSendToPythonRandomFace() : *
      {
         this.faceRandomize(false,true);
      }
      
      protected function enabledCharButtons(value:Boolean) : *
      {
         if(this.createButton != null)
         {
            this.updateCreateButton();
         }
         if(this.backButton != null)
         {
            this.backButton.enabled = value;
         }
         if(this.faceButton != null)
         {
            this.faceButton.enabled = value;
         }
         if(this.clothButton != null)
         {
            this.clothButton.enabled = value && !this._donat_only_face;
         }
         if(this.resetButton != null)
         {
            this.resetButton.enabled = value && !this._donat_only_face;
         }
         if(this.clothResetButton != null)
         {
            this.clothResetButton.enabled = value;
         }
         if(this.faceResetButton != null)
         {
            this.faceResetButton.enabled = value;
         }
         if(this.faceSaveButton != null)
         {
            this.faceSaveButton.enabled = value;
         }
         if(this.faceLoadButton != null)
         {
            this.faceLoadButton.enabled = value;
         }
         if(this.randomButton != null)
         {
            this.randomButton.enabled = value && !this._donat_only_face;
         }
      }
      
      protected function onLockCharButtons(event:ApiEvent) : *
      {
         var value:* = !event.data.answer["value"];
         this._create_flag_python = value;
         this.enabledCharButtons(value);
      }
      
      protected function onFaceChanged(event:ApiEvent, t:String = "") : *
      {
         dispatchEvent(new Event("onFaceChanged"));
      }
      
      protected function onRealyFaceChanged(event:Event) : *
      {
         this._change_flag = true;
         this.updateCreateButton();
      }
      
      protected function checkAccessLevelForButtons() : *
      {
         this.faceSaveButton.enabled = Boolean(Auth.self.access_level);
         this.faceLoadButton.enabled = Boolean(Auth.self.access_level);
         this.faceSaveButton.visible = Boolean(Auth.self.access_level);
         this.faceLoadButton.visible = Boolean(Auth.self.access_level);
      }
      
      protected function onAccessLevelChange(event:ApiEvent) : *
      {
         var level:Number = 0;
         if(typeof event.data.answer == "number")
         {
            level = Number(event.data.answer);
         }
         else
         {
            level = Number(event.data.answer["access_level"]);
         }
         if(this.faceSaveButton)
         {
            this.faceSaveButton.enabled = Boolean(level);
            this.faceSaveButton.visible = Boolean(Auth.self.access_level);
         }
         if(this.faceLoadButton)
         {
            this.faceLoadButton.enabled = Boolean(level);
            this.faceLoadButton.visible = Boolean(Auth.self.access_level);
         }
      }
      
      protected function createControlItem(parent:DisplayObjectContainer, item:Object) : *
      {
         if(item == null)
         {
            return;
         }
         switch(item.type)
         {
            case "ColorList":
               this.faceDict["data"][item.id] = this.createColorList(parent,item);
               break;
            case "Slider":
               this.faceDict["data"][item.id] = this.createSlider(parent,item);
               break;
            case "PictureButtonList":
               this.faceDict["data"][item.id] = this.createPictureButtonList(parent,item);
               break;
            case "SliderList":
               this.createSliderList(parent,item);
         }
      }
      
      protected function face_call(args:Array) : *
      {
         this.resetButton.enabled = true;
         this.faceResetButton.enabled = true;
         Api.call(Api.FACE_CHAR_VIEW,args);
      }
      
      protected function cloth_call(args:Array) : *
      {
         this.resetButton.enabled = true;
         this.clothResetButton.enabled = true;
         Api.call(Api.NEW_CHAR_VIEW,args);
      }
   }
}

