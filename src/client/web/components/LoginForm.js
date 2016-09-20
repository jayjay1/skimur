import React, { Component } from 'react';
import { action, observable, reaction, computed } from 'mobx';
import { inject, observer } from 'mobx-react';
import { logIn, sendCode, verifyCode, externalAuthentication } from 'actions';
import Input from 'components/Input';
import ExternalLoginButton from 'components/ExternalLoginButton';
import ErrorList from 'components/ErrorList';
import { injectActions } from 'helpers/decorators';
import _ from 'lodash';
import { Row, Col } from 'react-bootstrap';
import { observableForm, observableFormField } from 'utils/state/form';

@observableForm
class LoginFormState {
  @observableFormField userName = '';
  @observableFormField password = '';
  @observableFormField rememberMe = true;
}

@observableForm
class SendCodeFormState {
  @observableFormField provider = '';
}

@observableForm
class VerifyCodeFormState {
  @observableFormField code = ''
  @observableFormField rememberMe = true
  @observableFormField rememberBrowser = true
}

class LoginFormContainerState {

  constructor(actions) {
    this.actions = actions;
  }

  loginForm = new LoginFormState();
  sendCodeForm = new SendCodeFormState();
  verifyCodeForm = new VerifyCodeFormState();
  @observable requiresTwoFactor = false;
  @observable userFactors = [];
  @observable sentCode = false;
  @observable codeSentWithProvider;
  @observable codeVerified = false;

  @action logIn() {
    return this.actions.logIn(this.loginForm.userName.value, this.loginForm.password.value)
      .then(result => {
        this.handleLogin(result);
        return result;
      });
  }

  @action
  externalLogIn(scheme) {
    return this.actions.externalAuthentication(scheme)
      .then(result => {
        this.handleExternalLogin(result);
        return result;
      });
  }

  @action
  sendCode() {
    // the initial state is emtpy if the value was never changed.
    let provider = this.sendCodeForm.provider.value;
    if(provider === "" && !_.isEmpty(this.userFactors)) {
      provider = this.userFactors[0];
    }

    return this.actions.sendCode(provider).then(result => {
      this.handleSendCode(result);
    });
  }

  @action
  verifyCode() {
    return this.actions.verifyCode(this.codeSentWithProvider, this.verifyCodeForm.code.value, this.verifyCodeForm.rememberMe.value, this.verifyCodeForm.rememberBrowser.value)
      .then(result => {
        this.handleVerifyCode(result);
        return result;
      });
  }

  @action
  handleLogin(result) {
    this.loginForm.updateModelState(result);
    if(result.requiresTwoFactor) {
      this.requiresTwoFactor = true;
      this.userFactors = result.userFactors;
    }
  }

  @action
  handleExternalLogin(result) {
    this.loginForm.updateModelState(result);
    if(result.requiresTwoFactor) {
      this.requiresTwoFactor = true;
      this.userFactors = result.userFactors;
    }
  }

  @action
  handleSendCode(result) {
    this.sendCodeForm.updateModelState(result);
    if(result.success) {
      this.sentCode = true;
      this.codeSentWithProvider = result.provider;
    }
  }

  @action
  handleVerifyCode(result) {
    this.verifyCodeForm.updateModelState(result);
    if(result.success) {
      this.codeVerified = true;
    }
  }
}


@observer
class LoginForm extends Component {

  onClick = (event) => {
    event.preventDefault();
    this.props.state.logIn()
      .then(result => {
        if(result.user) {
          this.props.onLoggedIn();
        }
        return result;
      });
  }

  onExternalLoginClick(scheme) {
    return (event) => {
      event.preventDefault();
      this.props.state.externalLogIn(scheme)
        .then(result => {
          if(result.user) {
            this.props.onLoggedIn();
          }
          return result;
        });
    };
  }

  render() {
    return (
      <form onSubmit={this.onClick} className="form-horizontal">
        {(this.props.loginProviders.length > 0) &&
          <Row>
            <Col md={2} />
            <Col md={10}>
              <p>
                {this.props.loginProviders.map((loginProvider, i) =>
                (
                  <span key={i}>
                    <ExternalLoginButton
                      scheme={loginProvider.scheme}
                      text={loginProvider.displayName}
                      onClick={this.onExternalLoginClick(loginProvider.scheme)} />
                    {' '}
                  </span>
                ))}
              </p>
              <p>Or...</p>
            </Col>
          </Row>
        }
        <ErrorList errors={this.props.state.loginForm.modelStateErrors} />
        <Input field={this.props.state.loginForm.userName} name="userName" label="User name" /> 
        <Input field={this.props.state.loginForm.password} name="password" type="password" label="Password" />
        <Input field={this.props.state.loginForm.rememberMe} name="rememberMe" type="checkbox" label="Remember me" />
        <div className="form-group">
          <div className="col-md-offset-2 col-md-10">
            <button type="submit" className="btn btn-default">Login</button>
          </div>
        </div>
      </form>
    );
  }
}

@observer
class SendCodeForm extends Component {

  onClick = (event) => {
    event.preventDefault();
    this.props.state.sendCode();
  }

  render() {
    return (
      <form onSubmit={this.onClick} className="form-horizontal">
        <ErrorList errors={this.props.state.sendCodeForm.modelStateErrors} /> 
        <Input field={this.props.state.sendCodeForm.provider} 
          type="option"
          options={this.props.state.userFactors.map((userFactor) => ({ value: userFactor, display: userFactor }))}
          name="provider"
          label="Provider" />
        <div className="form-group">
          <div className="col-md-offset-2 col-md-10">
            <button type="submit" className="btn btn-default">Send</button>
          </div>
        </div>
      </form>
    );
  }
}

@observer
class VerifiyCodeForm extends Component {

  onClick = (event) => {
    event.preventDefault();
    this.props.state.verifyCode()
      .then(result => {
          if(result.success) {
            this.props.onLoggedIn();
          }
          return result;
      });
  }

  render() {
    return (
      <form onSubmit={this.onClick} className="form-horizontal">
        <ErrorList errors={this.props.state.verifyCodeForm.modelStateErrors} /> 
        <Input field={this.props.state.verifyCodeForm.code}
          name="code"
          label="Code" /> 
        <Input field={this.props.state.verifyCodeForm.rememberMe}
          type='checkbox'
          name="rememberMe"
          label="Remember me" />
        <Input field={this.props.state.verifyCodeForm.rememberBrowser}
          type='checkbox'
          name="rememberBrowser"
          label="Remember browser" /> 
        <div className="form-group">
          <div className="col-md-offset-2 col-md-10">
            <button type="submit" className="btn btn-default">Verify</button>
          </div>
        </div>
      </form>
    );
  }
}

@inject("store")
@injectActions({ logIn, sendCode, verifyCode, externalAuthentication }, "store")
@observer
export default class LoginFormContainer extends Component {
  constructor(props) {
    super(props);
    this.state = new LoginFormContainerState(props.actions);
  }

  onLoggedIn = () => {
    if(this.props.onLoggedIn) {
      this.props.onLoggedIn();
    }
  }

  render() {    
    if(this.state.codeVerified)
      return <div>You have been logged in!</div>

    if(this.state.sentCode)
      return <VerifiyCodeForm onLoggedIn={this.onLoggedIn} state={this.state} />

    if(this.state.requiresTwoFactor)
      return <SendCodeForm state={this.state}  />

    const {
      externalLogins : {
        loginProviders
      }
    } = this.props.store;
    return <LoginForm onLoggedIn={this.onLoggedIn} loginProviders={loginProviders} state={this.state}  />
  }
}